using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XRay.Parsers;

/// <summary>
/// Structural C#/.NET parser built on Roslyn syntax trees (no semantic/compilation model — fast,
/// dependency-free, best-effort). Extracts classes/interfaces, constructor-injection dependencies,
/// controller HTTP endpoints, and DbContext DbSet tables, matching the ComponentType/GraphEdgeType
/// vocabulary from schema.md.
/// </summary>
public class CSharpParser
{
    public ParseResult Parse(string relativeFilePath, string sourceText)
    {
        var nodes = new List<ParsedNode>();
        var edges = new List<ParsedEdge>();
        var errors = new List<string>();

        SyntaxTree tree;
        CompilationUnitSyntax root;
        try
        {
            tree = CSharpSyntaxTree.ParseText(sourceText);
            root = tree.GetCompilationUnitRoot();
        }
        catch (Exception ex)
        {
            errors.Add($"{relativeFilePath}: {ex.Message}");
            return new ParseResult(nodes, edges, errors);
        }

        var typeDeclarations = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

        foreach (var typeDecl in typeDeclarations)
        {
            var typeName = typeDecl.Identifier.Text;
            var isInterface = typeDecl is InterfaceDeclarationSyntax;
            var componentType = ClassifyType(typeName, isInterface, typeDecl);
            var key = BuildClassKey(typeDecl, typeName);

            var lineSpan = typeDecl.GetLocation().GetLineSpan();
            nodes.Add(new ParsedNode(
                key, componentType, typeName, relativeFilePath,
                lineSpan.StartLinePosition.Line + 1, lineSpan.EndLinePosition.Line + 1));

            // Inheritance / interface implementation
            if (typeDecl.BaseList is not null)
            {
                foreach (var baseType in typeDecl.BaseList.Types)
                {
                    var baseName = baseType.Type.ToString().Split('<')[0].Trim();
                    edges.Add(new ParsedEdge(
                        key, ResolveTypeKey(typeDecl, baseName), GraphEdgeTypeCodes.Inherits, 0.9m,
                        "csharp.base_list", relativeFilePath, lineSpan.StartLinePosition.Line + 1));
                }
            }

            // Constructor-injected dependencies
            var constructors = typeDecl.Members.OfType<ConstructorDeclarationSyntax>();
            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.ParameterList.Parameters)
                {
                    var paramTypeName = param.Type?.ToString().TrimStart('I') is { } t
                        ? param.Type!.ToString()
                        : null;
                    if (param.Type is null) continue;
                    var rawType = param.Type.ToString().Split('<')[0].Trim();
                    if (IsPrimitiveOrFramework(rawType)) continue;

                    var depLine = param.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    edges.Add(new ParsedEdge(
                        key, ResolveTypeKey(typeDecl, rawType), GraphEdgeTypeCodes.DependsOn, 0.85m,
                        "csharp.constructor_injection", relativeFilePath, depLine));
                }
            }

            // Controller HTTP endpoints
            if (componentType == ComponentTypeCodes.Controller)
            {
                var classRoute = GetAttributeArgument(typeDecl.AttributeLists, "Route");
                var controllerName = typeName.EndsWith("Controller", StringComparison.Ordinal)
                    ? typeName[..^"Controller".Length] : typeName;
                foreach (var method in typeDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    var httpVerb = GetHttpVerb(method.AttributeLists);
                    if (httpVerb is null) continue;
                    var (verb, attributeName) = httpVerb.Value;

                    var methodRoute = GetAttributeArgument(method.AttributeLists, attributeName) ?? "";
                    var fullPath = CombineRoute(classRoute, methodRoute, controllerName, method.Identifier.Text);
                    var apiKey = $"API:{verb} {fullPath}";
                    var apiLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                    nodes.Add(new ParsedNode(
                        apiKey, ComponentTypeCodes.Api, $"{verb} {fullPath}",
                        relativeFilePath, apiLine, apiLine));

                    edges.Add(new ParsedEdge(
                        apiKey, key, GraphEdgeTypeCodes.Exposes, 0.95m,
                        "csharp.http_attribute", relativeFilePath, apiLine));
                }
            }

            // DbContext DbSet<T> properties -> database table nodes
            if (IsDbContext(typeDecl))
            {
                foreach (var prop in typeDecl.Members.OfType<PropertyDeclarationSyntax>())
                {
                    if (prop.Type is not GenericNameSyntax { Identifier.Text: "DbSet" } generic) continue;
                    var entityName = generic.TypeArgumentList.Arguments.FirstOrDefault()?.ToString() ?? prop.Identifier.Text;
                    var tableKey = $"TABLE:{entityName}";
                    var propLine = prop.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

                    nodes.Add(new ParsedNode(tableKey, ComponentTypeCodes.DatabaseTable, entityName, relativeFilePath, propLine, propLine));
                    edges.Add(new ParsedEdge(
                        key, tableKey, GraphEdgeTypeCodes.DependsOn, 0.8m,
                        "csharp.dbset_property", relativeFilePath, propLine));
                }
            }

            // Repository/service member access on injected DbContext -> read/write table edges (best effort)
            foreach (var access in typeDecl.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var memberName = access.Name.Identifier.Text;
                // crude heuristic: `_context.Payments` / `dbContext.Orders` style access
                if (access.Expression is IdentifierNameSyntax { Identifier.Text: var recv } &&
                    LooksLikeDbContextReceiver(recv))
                {
                    var tableKey = $"TABLE:{memberName}";
                    var isWrite = access.Parent?.ToString().Contains("Add", StringComparison.OrdinalIgnoreCase) == true
                                  || access.Parent?.ToString().Contains("Remove", StringComparison.OrdinalIgnoreCase) == true
                                  || access.Parent?.ToString().Contains("Update", StringComparison.OrdinalIgnoreCase) == true;
                    var accessLine = access.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    edges.Add(new ParsedEdge(
                        key, tableKey,
                        isWrite ? GraphEdgeTypeCodes.Writes : GraphEdgeTypeCodes.Reads,
                        0.6m, "csharp.dbcontext_member_access", relativeFilePath, accessLine, IsRuntimeResolved: true));
                }
            }
        }

        return new ParseResult(nodes, edges, errors);
    }

    private static string BuildClassKey(TypeDeclarationSyntax typeDecl, string typeName)
    {
        var ns = GetNamespaceName(typeDecl);
        var enclosingPath = GetEnclosingTypeChain(typeDecl);
        var qualifiedName = string.IsNullOrEmpty(enclosingPath) ? typeName : $"{enclosingPath}.{typeName}";
        return string.IsNullOrEmpty(ns) ? $"CLASS:{qualifiedName}" : $"CLASS:{ns}.{qualifiedName}";
    }

    /// <summary>Dot-joined names of enclosing types, outermost first, so nested classes don't collide with top-level classes of the same name.</summary>
    private static string GetEnclosingTypeChain(TypeDeclarationSyntax typeDecl)
    {
        var names = new List<string>();
        for (var parent = typeDecl.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is TypeDeclarationSyntax enclosingType)
            {
                names.Add(enclosingType.Identifier.Text);
            }
        }
        names.Reverse();
        return string.Join(".", names);
    }

    private static bool LooksLikeDbContextReceiver(string receiver)
    {
        var normalized = receiver.TrimStart('_').ToLowerInvariant();
        return normalized is "db" or "context" or "dbcontext" || normalized.EndsWith("dbcontext", StringComparison.Ordinal);
    }

    private static string ResolveTypeKey(TypeDeclarationSyntax typeDecl, string rawType)
    {
        var typeName = rawType.Split('<')[0].Trim();
        if (string.IsNullOrWhiteSpace(typeName)) return "CLASS:UNKNOWN";
        if (typeName.Contains('.', StringComparison.Ordinal)) return $"CLASS:{typeName}";

        var ns = GetNamespaceName(typeDecl);
        return string.IsNullOrEmpty(ns) ? $"CLASS:{typeName}" : $"CLASS:{ns}.{typeName}";
    }

    private static string? GetNamespaceName(TypeDeclarationSyntax typeDecl)
    {
        for (var parent = typeDecl.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is NamespaceDeclarationSyntax namespaceDecl)
            {
                return namespaceDecl.Name.ToString();
            }

            if (parent is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                return fileScopedNamespace.Name.ToString();
            }
        }

        return null;
    }

    private static bool IsDbContext(TypeDeclarationSyntax typeDecl) =>
        typeDecl.BaseList?.Types.Any(t =>
        {
            var baseName = t.Type.ToString().Split('<')[0].Trim();
            return baseName.Equals("DbContext", StringComparison.Ordinal) || baseName.EndsWith("DbContext", StringComparison.Ordinal);
        }) == true;

    private static bool IsPrimitiveOrFramework(string typeName)
    {
        var known = new[]
        {
            "string", "int", "bool", "double", "decimal", "Guid", "DateTime", "object", "ILogger",
            "IConfiguration", "CancellationToken", "long", "float", "short", "byte"
        };
        return known.Any(k => typeName.StartsWith(k, StringComparison.Ordinal));
    }

    private static string ClassifyType(string typeName, bool isInterface, TypeDeclarationSyntax decl)
    {
        if (isInterface) return ComponentTypeCodes.Interface;
        if (typeName.EndsWith("Controller", StringComparison.Ordinal)) return ComponentTypeCodes.Controller;
        if (typeName.EndsWith("Repository", StringComparison.Ordinal)) return ComponentTypeCodes.Repository;
        if (typeName.EndsWith("Service", StringComparison.Ordinal) || typeName.EndsWith("Validator", StringComparison.Ordinal))
            return ComponentTypeCodes.Service;
        if (IsDbContext(decl)) return ComponentTypeCodes.Database;
        return ComponentTypeCodes.Unknown;
    }

    private static (string Verb, string AttributeName)? GetHttpVerb(SyntaxList<AttributeListSyntax> attrLists)
    {
        var map = new Dictionary<string, string>
        {
            ["HttpGet"] = "GET",
            ["HttpPost"] = "POST",
            ["HttpPut"] = "PUT",
            ["HttpDelete"] = "DELETE",
            ["HttpPatch"] = "PATCH",
        };
        foreach (var attrList in attrLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (map.TryGetValue(name, out var verb))
                {
                    return (verb, name);
                }
            }
        }
        return null;
    }

    private static string? GetAttributeArgument(SyntaxList<AttributeListSyntax> attrLists, string attributeName)
    {
        foreach (var attrList in attrLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                if (attr.Name.ToString() != attributeName) continue;
                var arg = attr.ArgumentList?.Arguments.FirstOrDefault();
                if (arg is null) return "";
                return arg.ToString().Trim('"');
            }
        }
        return null;
    }

    private static string CombineRoute(string? classRoute, string methodRoute, string controllerName, string actionName)
    {
        var basePath = (classRoute ?? "")
            .Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');
        var resolvedMethodRoute = methodRoute.Replace("[action]", actionName, StringComparison.OrdinalIgnoreCase);
        var full = $"/{basePath}/{resolvedMethodRoute}".Replace("//", "/");
        return full.TrimEnd('/') == "" ? "/" : full;
    }
}

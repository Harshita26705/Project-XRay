using System.Text.RegularExpressions;

namespace XRay.Parsers;

/// <summary>
/// Regex/heuristic TypeScript & TSX parser (no TS compiler dependency at runtime). Extracts React
/// components/modules, relative imports, and best-effort HTTP calls (fetch/axios) for cross-layer
/// linking against API nodes produced by <see cref="CSharpParser"/>.
/// </summary>
public partial class TypeScriptParser
{
    [GeneratedRegex(@"import\s+(?:[\w*{}\s,]+)\s+from\s+['""](\.[^'""]+)['""]", RegexOptions.Compiled)]
    private static partial Regex ImportRegex();

    [GeneratedRegex(@"(?:fetch|axios(?:\.(?:get|post|put|delete|patch))?)\s*\(\s*[`'""]([^`'""]+)[`'""]", RegexOptions.Compiled)]
    private static partial Regex HttpCallRegex();

    [GeneratedRegex(@"\b(GET|POST|PUT|DELETE|PATCH)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex VerbHintRegex();

    public ParseResult Parse(string relativeFilePath, string sourceText)
    {
        var nodes = new List<ParsedNode>();
        var edges = new List<ParsedEdge>();
        var errors = new List<string>();

        var isComponent = relativeFilePath.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase);
        var componentType = isComponent ? ComponentTypeCodes.FrontendComponent : ComponentTypeCodes.FrontendModule;
        var selfKey = $"MODULE:{relativeFilePath}";

        nodes.Add(new ParsedNode(selfKey, componentType, Path.GetFileNameWithoutExtension(relativeFilePath), relativeFilePath, 1, null));

        var lines = sourceText.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNo = i + 1;

            foreach (Match m in ImportRegex().Matches(line))
            {
                var importPath = ResolveRelativeImport(relativeFilePath, m.Groups[1].Value);
                edges.Add(new ParsedEdge(
                    selfKey, $"MODULE:{importPath}", GraphEdgeTypeCodes.Imports, 0.9m,
                    "typescript.import", relativeFilePath, lineNo));
            }

            foreach (Match m in HttpCallRegex().Matches(line))
            {
                var url = m.Groups[1].Value;
                var path = NormalizeApiPath(url);
                var verbMatch = VerbHintRegex().Match(line);
                var verb = verbMatch.Success ? verbMatch.Value.ToUpperInvariant() : "GET";
                var apiKey = $"API:{verb} {path}";

                edges.Add(new ParsedEdge(
                    selfKey, apiKey, GraphEdgeTypeCodes.Calls, 0.7m,
                    "typescript.http_call", relativeFilePath, lineNo, IsRuntimeResolved: true));
            }
        }

        return new ParseResult(nodes, edges, errors);
    }

    private static string ResolveRelativeImport(string fromFile, string relativeImport)
    {
        var dir = Path.GetDirectoryName(fromFile.Replace('/', Path.DirectorySeparatorChar)) ?? "";
        var combined = Path.GetFullPath(Path.Combine(dir, relativeImport)).Replace('\\', '/');
        // Strip any drive-root artifacts introduced by GetFullPath when the input is already relative.
        var marker = fromFile.Split('/')[0];
        var idx = combined.IndexOf(marker, StringComparison.Ordinal);
        return idx >= 0 ? combined[idx..] : relativeImport;
    }

    private static string NormalizeApiPath(string url)
    {
        // Strip origin/template noise like `${API_BASE}/api/payments/${id}` -> /api/payments/:id
        var withoutTemplate = Regex.Replace(url, @"\$\{[^}]+\}", ":param");
        var idx = withoutTemplate.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? withoutTemplate[idx..] : withoutTemplate;
    }
}

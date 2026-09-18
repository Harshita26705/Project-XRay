using System.Text.RegularExpressions;

namespace XRay.Parsers;

/// <summary>
/// Regex/heuristic TypeScript & TSX parser (no TS compiler dependency at runtime). Extracts React
/// components/modules, relative imports, and best-effort HTTP calls (fetch/axios) for cross-layer
/// linking against API nodes produced by <see cref="CSharpParser"/>.
/// </summary>
public partial class TypeScriptParser
{
    [GeneratedRegex(@"import\s+type\s*(?:[\w*{}\s,]+)\s+from\s+['""](\.[^'""]+)['""]|import\s+(?:[\w*{}\s,]+)\s+from\s+['""](\.[^'""]+)['""]", RegexOptions.Compiled)]
    private static partial Regex ImportRegex();

    [GeneratedRegex(@"(?<!\.)\bimport\s*\(\s*['""](\.[^'""]+)['""]\s*\)", RegexOptions.Compiled)]
    private static partial Regex DynamicImportRegex();

    [GeneratedRegex(@"(?:fetch|axios(?:\.(?<verb>get|post|put|delete|patch))?)\s*\(\s*[`'""](?<url>[^`'""]+)[`'""]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex HttpCallRegex();

    [GeneratedRegex(@"\bmethod\s*:\s*['""](GET|POST|PUT|DELETE|PATCH)['""]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
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

        // Matched against the whole file (not line-by-line) so multi-line imports/calls aren't missed.
        foreach (Match m in ImportRegex().Matches(sourceText))
        {
            var rawImport = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            var importPath = ResolveRelativeImport(relativeFilePath, rawImport);
            edges.Add(new ParsedEdge(
                selfKey, $"MODULE:{importPath}", GraphEdgeTypeCodes.Imports, 0.9m,
                "typescript.import", relativeFilePath, CountLines(sourceText, m.Index) + 1));
        }

        foreach (Match m in DynamicImportRegex().Matches(sourceText))
        {
            var importPath = ResolveRelativeImport(relativeFilePath, m.Groups[1].Value);
            edges.Add(new ParsedEdge(
                selfKey, $"MODULE:{importPath}", GraphEdgeTypeCodes.Imports, 0.85m,
                "typescript.dynamic_import", relativeFilePath, CountLines(sourceText, m.Index) + 1));
        }

        foreach (Match m in HttpCallRegex().Matches(sourceText))
        {
            var path = NormalizeApiPath(m.Groups["url"].Value);
            var verb = ResolveHttpVerb(m, sourceText);
            var apiKey = $"API:{verb} {path}";

            edges.Add(new ParsedEdge(
                selfKey, apiKey, GraphEdgeTypeCodes.Calls, 0.7m,
                "typescript.http_call", relativeFilePath, CountLines(sourceText, m.Index) + 1, IsRuntimeResolved: true));
        }

        return new ParseResult(nodes, edges, errors);
    }

    /// <summary>Prefers the explicit axios.verb() call over a nearby `method: '...'` hint, then defaults to GET.</summary>
    private static string ResolveHttpVerb(Match httpCallMatch, string sourceText)
    {
        if (httpCallMatch.Groups["verb"].Success) return httpCallMatch.Groups["verb"].Value.ToUpperInvariant();

        var windowEnd = Math.Min(sourceText.Length, httpCallMatch.Index + 300);
        var window = sourceText[httpCallMatch.Index..windowEnd];
        var hint = VerbHintRegex().Match(window);
        return hint.Success ? hint.Groups[1].Value.ToUpperInvariant() : "GET";
    }

    private static int CountLines(string text, int upToIndex) =>
        text[..Math.Min(upToIndex, text.Length)].Count(c => c == '\n');

    /// <summary>Pure string-segment resolution (no filesystem/CWD dependence) so duplicate directory names in the path can't confuse it.</summary>
    private static string ResolveRelativeImport(string fromFile, string relativeImport)
    {
        var segments = fromFile.Replace('\\', '/').Split('/').ToList();
        if (segments.Count > 0) segments.RemoveAt(segments.Count - 1); // drop the file name, keep the directory

        foreach (var segment in relativeImport.Replace('\\', '/').Split('/'))
        {
            if (segment.Length == 0 || segment == ".") continue;
            if (segment == "..")
            {
                if (segments.Count > 0) segments.RemoveAt(segments.Count - 1);
            }
            else
            {
                segments.Add(segment);
            }
        }

        return string.Join("/", segments);
    }

    private static string NormalizeApiPath(string url)
    {
        // Strip origin/template noise like `${API_BASE}/api/payments/${id}` -> /api/payments/:param
        var withoutTemplate = Regex.Replace(url, @"\$\{[^}]+\}", ":param");
        var withoutQuery = withoutTemplate.Split('?')[0];
        var idx = withoutQuery.IndexOf("/api", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? withoutQuery[idx..] : withoutQuery;
    }
}

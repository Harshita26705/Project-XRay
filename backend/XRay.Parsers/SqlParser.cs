using System.Text.RegularExpressions;

namespace XRay.Parsers;

/// <summary>
/// Regex-based T-SQL DDL parser (extracts CREATE TABLE / CREATE PROCEDURE definitions). The
/// Microsoft.SqlServer.TransactSql.ScriptDom package is referenced in this project for a future,
/// fully AST-accurate upgrade; this first pass favors speed and simplicity.
/// </summary>
public partial class SqlParser
{
    [GeneratedRegex(@"CREATE\s+TABLE\s+(?:\[?(\w+)\]?\.)?\[?(\w+)\]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CreateTableRegex();

    [GeneratedRegex(@"CREATE\s+(?:OR\s+ALTER\s+)?PROC(?:EDURE)?\s+(?:\[?\w+\]?\.)?\[?(\w+)\]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CreateProcedureRegex();

    // Matches both table-level ("FOREIGN KEY (...) REFERENCES Target(Col)") and column-level
    // ("Col INT REFERENCES Target(Col)") constraints, since both forms always end in "REFERENCES table(".
    [GeneratedRegex(@"REFERENCES\s+(?:\[?(\w+)\]?\.)?\[?(\w+)\]?\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex ForeignKeyRegex();

    public ParseResult Parse(string relativeFilePath, string sourceText)
    {
        var nodes = new List<ParsedNode>();
        var edges = new List<ParsedEdge>();
        var errors = new List<string>();

        // Split on GO batches so foreign key lookups stay scoped to their own CREATE TABLE statement.
        var batches = Regex.Split(sourceText, @"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var lineOffset = 0;

        foreach (var batch in batches)
        {
            var tableMatch = CreateTableRegex().Match(batch);
            if (tableMatch.Success)
            {
                var tableKey = $"TABLE:{QualifiedName(tableMatch.Groups[1], tableMatch.Groups[2].Value)}";
                var line = lineOffset + CountLines(batch, tableMatch.Index);
                nodes.Add(new ParsedNode(tableKey, ComponentTypeCodes.DatabaseTable, tableMatch.Groups[2].Value, relativeFilePath, line, null));

                foreach (Match fk in ForeignKeyRegex().Matches(batch))
                {
                    var targetKey = $"TABLE:{QualifiedName(fk.Groups[1], fk.Groups[2].Value)}";
                    edges.Add(new ParsedEdge(
                        tableKey, targetKey, GraphEdgeTypeCodes.References, 0.85m,
                        "sql.foreign_key", relativeFilePath, line));
                }
            }

            var procMatch = CreateProcedureRegex().Match(batch);
            if (procMatch.Success)
            {
                var procName = procMatch.Groups[1].Value;
                var procKey = $"PROC:{procName}";
                var line = lineOffset + CountLines(batch, procMatch.Index);
                nodes.Add(new ParsedNode(procKey, ComponentTypeCodes.StoredProcedure, procName, relativeFilePath, line, null));
            }

            lineOffset += CountLines(batch, batch.Length);
        }

        return new ParseResult(nodes, edges, errors);
    }

    /// <summary>Qualifies with the schema unless it's the implicit default "dbo", so multi-schema tables don't collide.</summary>
    private static string QualifiedName(Group schemaGroup, string tableName)
    {
        if (!schemaGroup.Success || schemaGroup.Value.Equals("dbo", StringComparison.OrdinalIgnoreCase))
        {
            return tableName;
        }
        return $"{schemaGroup.Value}.{tableName}";
    }

    private static int CountLines(string text, int upToIndex) =>
        text[..Math.Min(upToIndex, text.Length)].Count(c => c == '\n');
}

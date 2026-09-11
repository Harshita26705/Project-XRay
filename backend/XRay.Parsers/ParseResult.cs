namespace XRay.Parsers;

/// <summary>Component classification mirrors schema.md's ComponentType reference table codes.</summary>
public static class ComponentTypeCodes
{
    public const string Frontend = "FRONTEND";
    public const string FrontendModule = "FRONTEND_MODULE";
    public const string FrontendComponent = "FRONTEND_COMPONENT";
    public const string Api = "API";
    public const string Controller = "CONTROLLER";
    public const string Service = "SERVICE";
    public const string Interface = "INTERFACE";
    public const string Repository = "REPOSITORY";
    public const string Database = "DATABASE";
    public const string DatabaseTable = "DATABASE_TABLE";
    public const string DatabaseColumn = "DATABASE_COLUMN";
    public const string StoredProcedure = "STORED_PROCEDURE";
    public const string External = "EXTERNAL";
    public const string Unknown = "UNKNOWN";
}

public static class GraphEdgeTypeCodes
{
    public const string Calls = "CALLS";
    public const string DependsOn = "DEPENDS_ON";
    public const string Imports = "IMPORTS";
    public const string Exposes = "EXPOSES";
    public const string Reads = "READS";
    public const string Writes = "WRITES";
    public const string Binds = "BINDS";
    public const string Inherits = "INHERITS";
    public const string References = "REFERENCES";
    public const string Uses = "USES";
}

/// <summary>A parser-produced node, independent of EF Core entities (Parsers project has no Infrastructure reference).</summary>
public record ParsedNode(
    string ExternalKey,
    string ComponentTypeCode,
    string DisplayName,
    string? RelativeFilePath,
    int? StartLine,
    int? EndLine,
    bool IsPartial = false);

public record ParsedEdge(
    string SourceExternalKey,
    string TargetExternalKey,
    string EdgeTypeCode,
    decimal Confidence,
    string? ParserRule,
    string? RelativeFilePath,
    int? SourceLine,
    bool IsRuntimeResolved = false);

public record ParseResult(
    IReadOnlyList<ParsedNode> Nodes,
    IReadOnlyList<ParsedEdge> Edges,
    IReadOnlyList<string> Errors);

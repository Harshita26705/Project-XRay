using XRay.Domain.Projects;

namespace XRay.Domain.Ingestion;

public class IngestionRun
{
    public Guid IngestionRunId { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid RepositoryId { get; set; }
    public Repository? Repository { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string TriggerType { get; set; } = default!;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string StatusCode { get; set; } = default!;
    public string? ErrorMessage { get; set; }
    public int FilesDiscovered { get; set; }
    public int FilesParsed { get; set; }
    public int FilesFailed { get; set; }
}

public class CodeFile
{
    public Guid CodeFileId { get; set; }
    public Guid RepositoryId { get; set; }
    public Repository? Repository { get; set; }
    public string RelativePath { get; set; } = default!;
    public string? LanguageCode { get; set; }
    public string? FileExtension { get; set; }
    public bool IsDeleted { get; set; }
    public string? CurrentContentHash { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CodeFileVersion
{
    public Guid CodeFileVersionId { get; set; }
    public Guid CodeFileId { get; set; }
    public CodeFile? CodeFile { get; set; }
    public Guid IngestionRunId { get; set; }
    public IngestionRun? IngestionRun { get; set; }
    public string ContentHash { get; set; } = default!;
    public string? ContentUri { get; set; }
    public string? MaskedContentUri { get; set; }
    public string ParserStatusCode { get; set; } = default!;
    public string? ParserError { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CodeSymbol
{
    public Guid CodeSymbolId { get; set; }
    public Guid CodeFileVersionId { get; set; }
    public CodeFileVersion? CodeFileVersion { get; set; }
    public byte ComponentTypeId { get; set; }
    public string SymbolName { get; set; } = default!;
    public string? FullyQualifiedName { get; set; }
    public string? Signature { get; set; }
    public int? StartLine { get; set; }
    public int? StartColumn { get; set; }
    public int? EndLine { get; set; }
    public int? EndColumn { get; set; }
    public bool IsPartial { get; set; }
}

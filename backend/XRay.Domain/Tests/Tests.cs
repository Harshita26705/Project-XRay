using XRay.Domain.Analysis;
using XRay.Domain.Ingestion;

namespace XRay.Domain.Tests;

public class TestRun
{
    public Guid TestRunId { get; set; }
    public Guid AnalysisId { get; set; }
    public XRay.Domain.Analysis.Analysis? Analysis { get; set; }
    public string? FrameworkCode { get; set; }
    public string StatusCode { get; set; } = default!;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int PassedCount { get; set; }
    public int FailedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TestResult
{
    public Guid TestResultId { get; set; }
    public Guid TestRunId { get; set; }
    public TestRun? TestRun { get; set; }
    public string TestName { get; set; } = default!;
    public string StatusCode { get; set; } = default!;
    public long? DurationMs { get; set; }
    public string? FailureMessage { get; set; }
    public Guid? CodeFileId { get; set; }
    public CodeFile? CodeFile { get; set; }
}

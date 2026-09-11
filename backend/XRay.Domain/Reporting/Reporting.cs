using XRay.Domain.Analysis;
using XRay.Domain.Identity;

namespace XRay.Domain.Reporting;

public class Report
{
    public Guid ReportId { get; set; }
    public Guid AnalysisId { get; set; }
    public XRay.Domain.Analysis.Analysis? Analysis { get; set; }
    public string Title { get; set; } = default!;
    public string FormatCode { get; set; } = default!;
    public Guid? GeneratedByUserId { get; set; }
    public UserAccount? GeneratedByUser { get; set; }
    public string? StorageUri { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<ReportSection> Sections { get; set; } = new();
}

public class ReportSection
{
    public Guid ReportSectionId { get; set; }
    public Guid ReportId { get; set; }
    public Report? Report { get; set; }
    public string SectionTypeCode { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Content { get; set; }
    public int SortOrder { get; set; }
}

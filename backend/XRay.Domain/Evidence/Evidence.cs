using XRay.Domain.Analysis;
using XRay.Domain.Graph;
using XRay.Domain.Ingestion;

namespace XRay.Domain.EvidenceModel;

/// <summary>Named EvidenceModel namespace because "Evidence" is also the class name.</summary>
public class Evidence
{
    public Guid EvidenceId { get; set; }
    public Guid AnalysisId { get; set; }
    public XRay.Domain.Analysis.Analysis? Analysis { get; set; }
    public byte EvidenceTypeId { get; set; }
    public Guid? GraphNodeId { get; set; }
    public GraphNode? GraphNode { get; set; }
    public Guid? GraphEdgeId { get; set; }
    public GraphEdge? GraphEdge { get; set; }
    public Guid? ImpactPathId { get; set; }
    public ImpactPath? ImpactPath { get; set; }
    public Guid? CodeFileId { get; set; }
    public CodeFile? CodeFile { get; set; }
    public Guid? CodeFileVersionId { get; set; }
    public CodeFileVersion? CodeFileVersion { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public int? SourceLineStart { get; set; }
    public int? SourceLineEnd { get; set; }
    public string? ParserRule { get; set; }
    public decimal? Confidence { get; set; }
    public string? SourceSnippet { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

using XRay.Domain.Analysis;
using XRay.Domain.Graph;
using XRay.Domain.Security;

namespace XRay.Domain.Recommendations;

public class Recommendation
{
    public Guid RecommendationId { get; set; }
    public Guid AnalysisId { get; set; }
    public XRay.Domain.Analysis.Analysis? Analysis { get; set; }
    public Guid? SecurityFindingId { get; set; }
    public SecurityFinding? SecurityFinding { get; set; }
    public Guid? GraphNodeId { get; set; }
    public GraphNode? GraphNode { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string? PriorityCode { get; set; }
    public string SourceCode { get; set; } = default!;
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

using XRay.Domain.Ingestion;
using XRay.Domain.Projects;

namespace XRay.Domain.Graph;

public class GraphSnapshot
{
    public Guid GraphSnapshotId { get; set; }
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid IngestionRunId { get; set; }
    public IngestionRun? IngestionRun { get; set; }
    public bool IsCurrent { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}

public class GraphNode
{
    public Guid GraphNodeId { get; set; }
    public Guid GraphSnapshotId { get; set; }
    public GraphSnapshot? GraphSnapshot { get; set; }
    public byte ComponentTypeId { get; set; }
    public string ExternalKey { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public Guid? CodeSymbolId { get; set; }
    public CodeSymbol? CodeSymbol { get; set; }
    public Guid? CodeFileId { get; set; }
    public CodeFile? CodeFile { get; set; }
    public bool IsParsed { get; set; } = true;
    public bool IsPartial { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class GraphEdge
{
    public Guid GraphEdgeId { get; set; }
    public Guid GraphSnapshotId { get; set; }
    public GraphSnapshot? GraphSnapshot { get; set; }
    public byte GraphEdgeTypeId { get; set; }
    public Guid SourceNodeId { get; set; }
    public GraphNode? SourceNode { get; set; }
    public Guid TargetNodeId { get; set; }
    public GraphNode? TargetNode { get; set; }
    public decimal Confidence { get; set; }
    public decimal TraversalCost { get; set; }
    public bool IsRuntimeResolved { get; set; }
    public string? ParserRule { get; set; }
    public Guid? SourceCodeFileId { get; set; }
    public CodeFile? SourceCodeFile { get; set; }
    public int? SourceLine { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

namespace XRay.Api.Contracts;

public record CreateProjectRequest(string Name, string? Description, string? ExternalProjectUrl, string? LocalRepositoryPath);

public record ProjectResponse(
    Guid ProjectId,
    string Name,
    string? Description,
    bool IsActive,
    string? RepositoryReference,
    string ConnectionStatus,
    int NodeCount,
    int RelationshipCount,
    int SecurityFindingCount,
    DateTime? LastIndexedAtUtc,
    IReadOnlyList<string> TechnologyTags);

public record GraphNodeResponse(Guid NodeId, string ExternalKey, string ComponentType, string DisplayName, string? FilePath, bool IsParsed);

public record GraphEdgeResponse(Guid EdgeId, Guid SourceNodeId, Guid TargetNodeId, string EdgeType, decimal Confidence, bool IsRuntimeResolved);

public record GraphResponse(Guid? SnapshotId, IReadOnlyList<GraphNodeResponse> Nodes, IReadOnlyList<GraphEdgeResponse> Edges);

public record IngestRequest(string? LocalRepositoryPath);

public record IngestResponse(Guid IngestionRunId, int FilesDiscovered, int FilesParsed, int FilesFailed, int NodesCreated, int EdgesCreated, IReadOnlyList<string> Errors);

public record OverviewResponse(
    int TotalAnalyses,
    int CriticalChanges,
    int ComponentsAnalyzed,
    int SecurityFindings,
    IReadOnlyList<RecentAnalysisResponse> RecentAnalyses);

public record RecentAnalysisResponse(
    Guid AnalysisId,
    string ChangeTitle,
    string ChangeType,
    string? RiskState,
    int AffectedComponents,
    DateTime CreatedAtUtc,
    string Status);

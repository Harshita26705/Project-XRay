namespace XRay.Api.Contracts;

public record CreateProjectRequest(string Name, string? Description, string? ExternalProjectUrl, string? LocalRepositoryPath);

public record UpdateProjectRequest(string Name, string? Description);

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

public record NodeConnectionDetail(Guid NeighborNodeId, string NeighborName, string EdgeType, decimal Confidence, string Direction);

public record ProjectNodeDetailResponse(
    Guid NodeId,
    string ExternalKey,
    string ComponentType,
    string DisplayName,
    string? FilePath,
    bool IsParsed,
    IReadOnlyList<string> Contains,
    IReadOnlyList<NodeConnectionDetail> Incoming,
    IReadOnlyList<NodeConnectionDetail> Outgoing,
    string? WhyThisMatters);

public record ProjectSecurityScanResponse(Guid SecurityScanId, Guid ProjectId, int FindingCount, string Status, DateTime StartedAtUtc, DateTime? CompletedAtUtc);

public record GraphResponse(Guid? SnapshotId, IReadOnlyList<GraphNodeResponse> Nodes, IReadOnlyList<GraphEdgeResponse> Edges);

public record IngestRequest(string? LocalRepositoryPath, string? BranchName);

public record BranchResponse(string Name, string? HeadCommitSha, bool IsDefault, bool IsIndexed);

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

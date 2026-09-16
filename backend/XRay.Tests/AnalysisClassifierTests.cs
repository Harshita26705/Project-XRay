using XRay.Api.Services;
using XRay.Domain.Analysis;
using XRay.Domain.Graph;

namespace XRay.Tests;

public class AnalysisClassifierTests
{
    [Fact]
    public void Classify_UnparsedNode_UsesR1()
    {
        var result = Classify(CreateNode(isParsed: false), false);

        Assert.Equal(("UNKNOWN", "R1"), (result.RiskCode, result.RuleCode));
    }

    [Fact]
    public void Classify_ChangedPartialNode_UsesR2()
    {
        var result = Classify(CreateNode(isPartial: true), true);

        Assert.Equal(("UNKNOWN", "R2"), (result.RiskCode, result.RuleCode));
    }

    [Fact]
    public void Classify_DirectlyChangedNode_UsesR3()
    {
        var result = Classify(CreateNode(), true);

        Assert.Equal(("CRITICAL", "R3"), (result.RiskCode, result.RuleCode));
    }

    [Fact]
    public void ApplySecurityFindingOverride_HighSeverity_UsesR4()
    {
        var result = new AnalysisNodeResult { GraphNodeId = Guid.NewGuid(), RiskStateId = 3 };

        AnalysisService.ApplySecurityFindingOverride("HIGH", result, criticalRiskStateId: 1);

        Assert.Equal(1, result.RiskStateId);
        Assert.Equal("R4", result.RuleCode);
        Assert.True(result.IsSecurityAffected);
    }

    [Fact]
    public void Classify_OneHopAtThreshold_UsesR5()
    {
        var result = Classify(CreateNode(), false, distance: 1, confidence: 0.70m);

        Assert.Equal(("CRITICAL", "R5"), (result.RiskCode, result.RuleCode));
    }

    [Fact]
    public void Classify_OneHopBelowThreshold_UsesR6()
    {
        var result = Classify(CreateNode(), false, distance: 1, confidence: 0.69m);

        Assert.Equal(("RISKY", "R6"), (result.RiskCode, result.RuleCode));
    }

    [Fact]
    public void Classify_IndirectReachableNode_UsesR8()
    {
        var result = Classify(CreateNode(), false, distance: 2, confidence: 0.90m);

        Assert.Equal(("RISKY", "R8"), (result.RiskCode, result.RuleCode));
    }

    [Fact]
    public void Classify_RuntimeResolvedPath_UsesR9()
    {
        var result = Classify(CreateNode(), false, distance: 1, confidence: 0.95m, runtimeResolved: true);

        Assert.Equal(("UNKNOWN", "R9"), (result.RiskCode, result.RuleCode));
    }

    [Fact]
    public void Classify_ReachablePartialNode_UsesR10()
    {
        var result = Classify(CreateNode(isPartial: true), false, distance: 2, confidence: 0.90m);

        Assert.Equal(("UNKNOWN", "R10"), (result.RiskCode, result.RuleCode));
    }

    [Fact]
    public void Classify_BeyondDepthNode_UsesR11()
    {
        var node = CreateNode();
        var result = AnalysisService.Classify(
            node,
            false,
            new Dictionary<Guid, int>(),
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, bool>(),
            new HashSet<Guid> { node.GraphNodeId });

        Assert.Equal(("RISKY", "R11"), (result.RiskCode, result.RuleCode));
        Assert.True(result.IsBeyond);
    }

    [Fact]
    public void Classify_UnreachableParsedNode_UsesR12()
    {
        var result = Classify(CreateNode(), false);

        Assert.Equal(("SAFE", "R12"), (result.RiskCode, result.RuleCode));
    }

    private static (string RiskCode, string RuleCode, int? Distance, decimal? Confidence, bool IsRuntime, bool IsBeyond) Classify(
        GraphNode node,
        bool isChanged,
        int? distance = null,
        decimal? confidence = null,
        bool runtimeResolved = false)
    {
        var distances = distance is null ? new Dictionary<Guid, int>() : new() { [node.GraphNodeId] = distance.Value };
        var confidences = confidence is null ? new Dictionary<Guid, decimal>() : new() { [node.GraphNodeId] = confidence.Value };
        var runtime = new Dictionary<Guid, bool> { [node.GraphNodeId] = runtimeResolved };
        return AnalysisService.Classify(node, isChanged, distances, confidences, runtime, new HashSet<Guid>());
    }

    private static GraphNode CreateNode(bool isParsed = true, bool isPartial = false) => new()
    {
        GraphNodeId = Guid.NewGuid(),
        GraphSnapshotId = Guid.NewGuid(),
        ComponentTypeId = 1,
        ExternalKey = "CLASS:Test.Component",
        DisplayName = "Test.Component",
        IsParsed = isParsed,
        IsPartial = isPartial,
        CreatedAtUtc = DateTime.UtcNow,
    };
}
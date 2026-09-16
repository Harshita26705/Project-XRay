using XRay.Api.Services;
using XRay.Domain.Graph;
using XRay.Parsers;

namespace XRay.Tests;

public class Phase2RegressionTests
{
    [Fact]
    public void CSharpParser_UsesNamespaceQualifiedClassKeys()
    {
        var parser = new CSharpParser();
        var result = parser.Parse("Demo.cs", """
namespace Alpha
{
    public class UserService { }
}

namespace Beta
{
    public class UserService { }
}
""");

        var classKeys = result.Nodes
            .Select(node => node.ExternalKey)
            .Where(key => key.StartsWith("CLASS:", StringComparison.Ordinal))
            .OrderBy(key => key)
            .ToArray();

        Assert.Equal(new[]
        {
            "CLASS:Alpha.UserService",
            "CLASS:Beta.UserService"
        }, classKeys);
    }

    [Fact]
    public void ComputeDeterministicReachability_IsStableAcrossEquivalentPaths()
    {
        var changedId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var throughA = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var throughB = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var target = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var edges = new[]
        {
            CreateEdge(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), throughA, changedId, 0.90m),
            CreateEdge(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), throughB, changedId, 0.75m),
            CreateEdge(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), target, throughA, 0.80m),
            CreateEdge(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), target, throughB, 0.80m),
        };

        var result1 = AnalysisService.ComputeDeterministicReachability(edges, new[] { changedId }, maxDepth: 5);
        var result2 = AnalysisService.ComputeDeterministicReachability(edges.Reverse().ToArray(), new[] { changedId }, maxDepth: 5);

        Assert.Equal(result1.BestDistance[target], result2.BestDistance[target]);
        Assert.Equal(
            result1.BestPathEdges[target].Select(edge => edge.GraphEdgeId),
            result2.BestPathEdges[target].Select(edge => edge.GraphEdgeId));
    }

    private static GraphEdge CreateEdge(Guid edgeId, Guid sourceNodeId, Guid targetNodeId, decimal confidence)
    {
        return new GraphEdge
        {
            GraphEdgeId = edgeId,
            GraphSnapshotId = Guid.NewGuid(),
            SourceNodeId = sourceNodeId,
            TargetNodeId = targetNodeId,
            GraphEdgeTypeId = 1,
            Confidence = confidence,
            TraversalCost = 1m,
            ParserRule = "test.rule",
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}

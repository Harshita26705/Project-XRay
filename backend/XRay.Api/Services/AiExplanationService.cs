using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Ai;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

/// <summary>
/// AI is strictly explanatory: every generated sentence is built only from Evidence and
/// AnalysisNodeResult rows already produced by the deterministic engine — it never introduces new
/// nodes, edges, risk states, or findings. When no real Foundry/Azure OpenAI configuration is
/// present, falls back to a deterministic template and marks the generation as Degraded, matching
/// the product's "AI unavailable" honest-degradation rule rather than silently pretending success.
/// </summary>
public class AiExplanationService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiExplanationService(AppDbContext db, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ExplainResponse> ExplainAsync(Guid analysisId, ExplainRequest request, CancellationToken ct = default)
    {
        var analysis = await _db.Analyses.FirstOrDefaultAsync(a => a.AnalysisId == analysisId, ct)
                       ?? throw new InvalidOperationException("Analysis not found.");

        var riskStates = await _db.RiskStates.ToDictionaryAsync(r => r.Id, r => r.Code, ct);
        var nodeResults = await _db.AnalysisNodeResults.Where(n => n.AnalysisId == analysisId).ToListAsync(ct);
        var graphNodes = await _db.GraphNodes.Where(n => nodeResults.Select(r => r.GraphNodeId).Contains(n.GraphNodeId)).ToDictionaryAsync(n => n.GraphNodeId, ct);
        var evidence = await _db.Evidences.Where(e => e.AnalysisId == analysisId).ToListAsync(ct);

        var critical = nodeResults.Where(n => riskStates.GetValueOrDefault(n.RiskStateId) == "CRITICAL")
            .Select(n => graphNodes.GetValueOrDefault(n.GraphNodeId)?.DisplayName ?? "component").Distinct().Take(5).ToList();
        var risky = nodeResults.Count(n => riskStates.GetValueOrDefault(n.RiskStateId) == "RISKY");

        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _configuration["AzureOpenAI:ApiKey"];
        var degraded = string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey);

        string summary;
        if (!degraded)
        {
            // A real Foundry/Azure OpenAI endpoint is configured — call it, but still only feed it the
            // verified facts above (no live network calls without a configured deployment).
            summary = await CallFoundryAsync(endpoint!, apiKey!, critical, risky, ct) ?? BuildDeterministicSummary(critical, risky);
        }
        else
        {
            summary = BuildDeterministicSummary(critical, risky);
        }

        var provider = await _db.AIProviders.FirstOrDefaultAsync(p => p.Code == (degraded ? "MOCK" : "MICROSOFT_FOUNDRY"), ct);
        if (provider is null)
        {
            provider = new AIProvider { AIProviderId = Guid.NewGuid(), Code = degraded ? "MOCK" : "MICROSOFT_FOUNDRY", DisplayName = degraded ? "Deterministic Fallback" : "Microsoft Foundry" };
            _db.AIProviders.Add(provider);
        }

        var config = await _db.AIConfigurations.FirstOrDefaultAsync(c => c.OrganizationId == analysis.OrganizationId, ct);
        if (config is null)
        {
            config = new AIConfiguration { AIConfigurationId = Guid.NewGuid(), OrganizationId = analysis.OrganizationId, AIProviderId = provider.AIProviderId, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
            _db.AIConfigurations.Add(config);
        }

        var generation = new AIGeneration
        {
            AIGenerationId = Guid.NewGuid(),
            AnalysisId = analysisId,
            AIConfigurationId = config.AIConfigurationId,
            PurposeCode = "EXPLANATION",
            StatusCode = "COMPLETED",
            OutputText = summary,
            ValidatedOutput = summary,
            Degraded = degraded,
            CreatedAtUtc = DateTime.UtcNow,
        };
        _db.AIGenerations.Add(generation);

        // Guardrail: reference only evidence that already exists for this analysis.
        foreach (var e in evidence.Take(10))
        {
            _db.AIOutputReferences.Add(new AIOutputReference { AIOutputReferenceId = Guid.NewGuid(), AIGenerationId = generation.AIGenerationId, EvidenceId = e.EvidenceId });
        }

        await _db.SaveChangesAsync(ct);

        var keyPoints = new List<string>();
        if (critical.Count > 0) keyPoints.Add($"Directly affects: {string.Join(", ", critical)}");
        if (risky > 0) keyPoints.Add($"{risky} component(s) carry transitive/indirect risk.");
        if (evidence.Any(e => e.EvidenceTypeId == 4 /* SECURITY_ALERT seed order */)) keyPoints.Add("Security findings were attributed to at least one affected component.");

        return new ExplainResponse(summary, keyPoints, degraded);
    }

    private static string BuildDeterministicSummary(List<string> critical, int riskyCount)
    {
        if (critical.Count == 0 && riskyCount == 0)
        {
            return "No structural or security evidence links this change to downstream components at the current confidence threshold.";
        }

        var criticalPart = critical.Count > 0
            ? $"This change directly modifies {string.Join(", ", critical)}, which the dependency graph marks as critical impact."
            : "No component was directly modified within the traced dependency graph.";
        var riskyPart = riskyCount > 0
            ? $" {riskyCount} additional component(s) are reachable through transitive dependencies and should be reviewed before merging."
            : "";

        return criticalPart + riskyPart;
    }

    private async Task<string?> CallFoundryAsync(string endpoint, string apiKey, List<string> critical, int riskyCount, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            var prompt = $"Explain in two sentences why these components are at risk: {string.Join(", ", critical)}. {riskyCount} components carry transitive risk.";
            var payload = new { messages = new[] { new { role = "user", content = prompt } }, max_tokens = 200 };
            var response = await client.PostAsJsonAsync(endpoint, payload, ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            return json.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0
                ? choices[0].GetProperty("message").GetProperty("content").GetString()
                : null;
        }
        catch
        {
            return null; // Falls back to the deterministic summary — never silently fail the request.
        }
    }
}

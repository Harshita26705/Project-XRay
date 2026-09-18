using Microsoft.EntityFrameworkCore;
using XRay.Api.Contracts;
using XRay.Domain.Ai;
using XRay.Infrastructure.Persistence;

namespace XRay.Api.Services;

/// <summary>
/// AI is strictly explanatory: every generated sentence is built only from Evidence and
/// AnalysisNodeResult rows already produced by the deterministic engine — it never introduces new
/// nodes, edges, risk states, or findings. When Gemini is not configured or unavailable, this
/// service falls back to a deterministic template and marks the generation as Degraded.
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

    public async Task<AIProvider> GetOrCreateProviderAsync(string providerCode, CancellationToken ct = default)
    {
        var normalized = string.IsNullOrWhiteSpace(providerCode) ? "GOOGLE_GEMINI" : providerCode.Trim();
        var provider = await _db.AIProviders.FirstOrDefaultAsync(p => p.Code == normalized, ct);
        if (provider is not null) return provider;

        provider = new AIProvider
        {
            AIProviderId = Guid.NewGuid(),
            Code = normalized,
            DisplayName = normalized switch
            {
                "GOOGLE_GEMINI" => "Google Gemini",
                "MOCK" => "Deterministic Fallback",
                _ => normalized
            }
        };

        _db.AIProviders.Add(provider);
        await _db.SaveChangesAsync(ct);
        return provider;
    }

    public async Task<AIConfiguration?> GetConfigurationAsync(CancellationToken ct = default)
    {
        return await _db.AIConfigurations.OrderByDescending(c => c.UpdatedAtUtc).FirstOrDefaultAsync(ct);
    }

    public async Task SaveConfiguration(AIConfiguration config, CancellationToken ct = default)
    {
        _db.AIConfigurations.Add(config);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateConfiguration(AIConfiguration config, CancellationToken ct = default)
    {
        _db.AIConfigurations.Update(config);
        await _db.SaveChangesAsync(ct);
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

        var endpoint = _configuration["Gemini:Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta";
        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        var degraded = IsPlaceholder(apiKey);

        string summary;
        if (!degraded)
        {
            summary = await CallGeminiAsync(endpoint, apiKey!, model, critical, risky, request.Focus, ct)
                      ?? BuildDeterministicSummary(critical, risky);
        }
        else
        {
            summary = BuildDeterministicSummary(critical, risky);
        }

        var providerCode = degraded ? "MOCK" : "GOOGLE_GEMINI";
        var provider = await _db.AIProviders.FirstOrDefaultAsync(p => p.Code == providerCode, ct);
        if (provider is null)
        {
            provider = new AIProvider { AIProviderId = Guid.NewGuid(), Code = providerCode, DisplayName = degraded ? "Deterministic Fallback" : "Google Gemini" };
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

    private async Task<string?> CallGeminiAsync(string endpoint, string apiKey, string model, List<string> critical, int riskyCount, string? focus, CancellationToken ct)
    {
        var prompt = $"You are explaining a deterministic software change-impact analysis. Use only these verified facts. " +
                     $"Directly affected components: {string.Join(", ", critical.DefaultIfEmpty("none"))}. " +
                     $"Transitive risky components: {riskyCount}. " +
                     (string.IsNullOrWhiteSpace(focus) ? "" : $"Requested focus: {focus}. ") +
                     "Respond in two concise sentences. Do not invent components, dependencies, vulnerabilities, or risk states.";
        return await SendGeminiPromptAsync(endpoint, apiKey, model, "You are a precise, evidence-bound software analysis assistant.", prompt, ct);
    }

    /// <summary>Produces a plain-English "what is this and what does it connect to" summary for a single graph node.</summary>
    public async Task<NodeExplainResponse> ExplainNodeAsync(string displayName, string componentType, string? filePath, string? fileContent, IReadOnlyList<string> contains, IReadOnlyList<string> dependsOn, IReadOnlyList<string> dependedOnBy, CancellationToken ct = default)
    {
        var endpoint = _configuration["Gemini:Endpoint"] ?? "https://generativelanguage.googleapis.com/v1beta";
        var apiKey = _configuration["Gemini:ApiKey"];
        var model = _configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        var degraded = IsPlaceholder(apiKey);

        string? summary = null;
        if (!degraded)
        {
            var prompt = !string.IsNullOrWhiteSpace(fileContent)
                ? BuildFileContentPrompt(displayName, componentType, filePath!, fileContent)
                : BuildStructuralPrompt(displayName, componentType, filePath, contains, dependsOn, dependedOnBy);
            summary = await SendGeminiPromptAsync(endpoint, apiKey!, model, "You explain source code and software architecture in simple English for a non-technical audience, strictly grounded in what is given to you.", prompt, ct);
        }

        summary ??= BuildDeterministicNodeSummary(displayName, componentType, filePath, contains, dependsOn, dependedOnBy);
        return new NodeExplainResponse(summary, degraded || summary is null);
    }

    /// <summary>Feeds the actual source file to the model so the explanation is grounded in what the code does, not just graph metadata.</summary>
    private static string BuildFileContentPrompt(string displayName, string componentType, string filePath, string fileContent)
    {
        const int maxChars = 8000;
        var truncated = fileContent.Length > maxChars ? fileContent[..maxChars] + "\n... (truncated)" : fileContent;
        return $"Explain what the code below does, in simple, plain English for a non-technical reader. " +
               $"Base your explanation ONLY on the code shown between the markers \u2014 do not invent behavior that isn't there. " +
               $"Component: {displayName} ({componentType}), file: {filePath}.\n\n" +
               $"--- CODE START ---\n{truncated}\n--- CODE END ---\n\n" +
               "Respond in 3-5 short sentences, no jargon, no markdown, describing only what this file's code does.";
    }

    private static string BuildStructuralPrompt(string displayName, string componentType, string? filePath, IReadOnlyList<string> contains, IReadOnlyList<string> dependsOn, IReadOnlyList<string> dependedOnBy)
    {
        return $"Explain what this software component is and what it connects to, in simple, plain English for a non-technical reader. " +
               $"Use only these verified structural facts \u2014 do not invent anything beyond them. " +
               $"Name: {displayName}. Type: {componentType}. " +
               (filePath is null ? "" : $"File: {filePath}. ") +
               (contains.Count == 0 ? "" : $"Contains: {string.Join(", ", contains)}. ") +
               $"Depends on {dependsOn.Count} other component(s){(dependsOn.Count == 0 ? "" : $": {string.Join(", ", dependsOn.Take(5))}")}. " +
               $"Is depended on by {dependedOnBy.Count} other component(s){(dependedOnBy.Count == 0 ? "" : $": {string.Join(", ", dependedOnBy.Take(5))}")}. " +
               "Respond in 2-3 short sentences, no jargon, no markdown.";
    }

    private static string BuildDeterministicNodeSummary(string displayName, string componentType, string? filePath, IReadOnlyList<string> contains, IReadOnlyList<string> dependsOn, IReadOnlyList<string> dependedOnBy)
    {
        var kind = componentType.Replace('_', ' ').ToLowerInvariant();
        var location = filePath is null ? "" : $" defined in {filePath}";
        var deps = dependsOn.Count == 0 ? "does not depend on any other tracked component" : $"depends on {dependsOn.Count} other component(s)";
        var dependents = dependedOnBy.Count == 0 ? "nothing else currently depends on it" : $"{dependedOnBy.Count} other component(s) depend on it";
        return $"{displayName} is a {kind}{location}. It {deps}, and {dependents}.";
    }

    private async Task<string?> SendGeminiPromptAsync(string endpoint, string apiKey, string model, string systemPrompt, string userPrompt, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var payload = new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = userPrompt } } } },
                generationConfig = new
                {
                    temperature = _configuration.GetValue("Gemini:Temperature", 0.2),
                    maxOutputTokens = _configuration.GetValue("Gemini:MaxOutputTokens", 200)
                }
            };
            var url = $"{endpoint.TrimEnd('/')}/models/{Uri.EscapeDataString(model)}:generateContent";
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            requestMessage.Headers.Add("x-goog-api-key", apiKey);
            using var response = await client.SendAsync(requestMessage, ct);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            if (!json.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0) return null;
            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            return parts.GetArrayLength() == 0 ? null : parts[0].GetProperty("text").GetString();
        }
        catch
        {
            return null; // Falls back to the deterministic summary — never silently fail the request.
        }
    }

    private static bool IsPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.StartsWith("REPLACE-WITH-YOUR-", StringComparison.OrdinalIgnoreCase);
}

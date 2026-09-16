using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Contracts;
using XRay.Api.Services;
using XRay.Domain.Ai;

namespace XRay.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class AiController : ControllerBase
{
    private readonly AiExplanationService _ai;
    private readonly IConfiguration _configuration;

    public AiController(AiExplanationService ai, IConfiguration configuration)
    {
        _ai = ai;
        _configuration = configuration;
    }

    [HttpPost("analyses/{analysisId:guid}/explain")]
    public async Task<ActionResult<ExplainResponse>> Explain(Guid analysisId, ExplainRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _ai.ExplainAsync(analysisId, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("ai/configuration")]
    public ActionResult<AiConfigurationResponse> Configuration()
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var configured = !string.IsNullOrWhiteSpace(apiKey) &&
                         !apiKey.StartsWith("REPLACE-WITH-YOUR-", StringComparison.OrdinalIgnoreCase);
        return Ok(new AiConfigurationResponse(
            "Google Gemini",
            configured ? "CONNECTED" : "NOT_CONFIGURED",
            configured ? _configuration["Gemini:Model"] ?? "gemini-2.5-flash" : null,
            "Azure AI Search"));
    }

    [HttpPut("ai/configuration")]
    public async Task<ActionResult<AiConfigurationResponse>> UpdateConfiguration(UpdateAiConfigurationRequest request, CancellationToken ct)
    {
        var providerCode = request.Provider;
        var provider = await _ai.GetOrCreateProviderAsync(providerCode, ct);

        var existing = await _ai.GetConfigurationAsync(ct);
        var config = existing ?? new AIConfiguration
        {
            AIConfigurationId = Guid.NewGuid(),
            OrganizationId = Guid.Empty,
            AIProviderId = provider.AIProviderId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        config.AIProviderId = provider.AIProviderId;
        config.ModelName = request.ActiveModel ?? config.ModelName ?? "gemini-2.5-flash";
        config.Temperature = request.Temperature ?? config.Temperature ?? 0.2m;
        config.MaxTokens = request.MaxTokens ?? config.MaxTokens ?? 200;
        config.DeploymentName = request.SystemPromptOverride ?? config.DeploymentName;
        config.UpdatedAtUtc = DateTime.UtcNow;

        if (existing is null)
        {
            await _ai.SaveConfiguration(config, ct);
        }
        else
        {
            await _ai.UpdateConfiguration(config, ct);
        }

        return Ok(new AiConfigurationResponse(
            provider.DisplayName,
            "CONNECTED",
            config.ModelName,
            "Azure AI Search"));
    }
}

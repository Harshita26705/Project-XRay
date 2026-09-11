using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XRay.Api.Contracts;
using XRay.Api.Services;

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
        var configured = !string.IsNullOrWhiteSpace(_configuration["AzureOpenAI:Endpoint"]);
        return Ok(new AiConfigurationResponse(
            "Microsoft Foundry",
            configured ? "CONNECTED" : "NOT_CONFIGURED",
            configured ? _configuration["AzureOpenAI:DeploymentName"] ?? "foundry-gpt4-turbo" : null,
            "Azure AI Search"));
    }
}

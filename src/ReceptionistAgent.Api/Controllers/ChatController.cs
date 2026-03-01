using ReceptionistAgent.AI.Agents;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Connectors.Repositories;
using ReceptionistAgent.Connectors.Security;
using ReceptionistAgent.Api.Security;
using ReceptionistAgent.Core.Security;
using ReceptionistAgent.Api.Services;
using ReceptionistAgent.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ReceptionistAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("Global")]
public class ChatController : ControllerBase
{
    private readonly IChatOrchestrator _orchestrator;
    private readonly TenantContext _tenantContext;

    public ChatController(
        IChatOrchestrator orchestrator,
        TenantContext tenantContext)
    {
        _orchestrator = orchestrator;
        _tenantContext = tenantContext;
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        if (!_tenantContext.IsResolved)
        {
            return BadRequest("Tenant no resuelto.");
        }

        var tenantId = _tenantContext.CurrentTenant!.TenantId;
        var sessionId = request.SessionId == Guid.Empty ? Guid.NewGuid() : request.SessionId;
        // Log del bloqueo de seguridad
        // ═══ Ejecutar pipeline mediante el orquestador ═══
        var result = await _orchestrator.ProcessMessageAsync(
            message: request.Message,
            sessionId: sessionId,
            tenantId: tenantId,
            eventTypePrefix: "" // Para chat normal no usamos prefijo, o podríamos usar "Api"
        );

        return Ok(new ChatResponse
        {
            SessionId = sessionId,
            Response = result.Response
        });
    }
}

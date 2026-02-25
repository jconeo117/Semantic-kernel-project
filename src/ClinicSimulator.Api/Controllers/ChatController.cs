using ClinicSimulator.AI.Agents;
using ClinicSimulator.Core.Adapters;
using ClinicSimulator.Core.Models;
using ClinicSimulator.Core.Repositories;
using ClinicSimulator.Core.Security;
using ClinicSimulator.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSimulator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly RecepcionistAgent _agent;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly TenantContext _tenantContext;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IClientDataAdapter _adapter;
    private readonly IInputGuard _inputGuard;
    private readonly IOutputFilter _outputFilter;
    private readonly IAuditLogger _auditLogger;

    public ChatController(
        RecepcionistAgent agent,
        IChatSessionRepository sessionRepository,
        TenantContext tenantContext,
        IPromptBuilder promptBuilder,
        IClientDataAdapter adapter,
        IInputGuard inputGuard,
        IOutputFilter outputFilter,
        IAuditLogger auditLogger)
    {
        _agent = agent;
        _sessionRepository = sessionRepository;
        _tenantContext = tenantContext;
        _promptBuilder = promptBuilder;
        _adapter = adapter;
        _inputGuard = inputGuard;
        _outputFilter = outputFilter;
        _auditLogger = auditLogger;
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

        // ═══ PASO 1: Input Guard - Detectar prompt injection ═══
        var guardResult = await _inputGuard.AnalyzeAsync(request.Message);

        // Log del mensaje del usuario (siempre)
        await _auditLogger.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            SessionId = sessionId,
            EventType = "UserMessage",
            Content = request.Message,
            ThreatLevel = guardResult.Level
        });

        if (!guardResult.IsAllowed)
        {
            // Log del bloqueo de seguridad
            await _auditLogger.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                SessionId = sessionId,
                EventType = "SecurityBlock",
                Content = guardResult.RejectionReason ?? "Mensaje bloqueado",
                ThreatLevel = guardResult.Level,
                Metadata = new() { ["originalMessage"] = request.Message }
            });

            // Retornar respuesta genérica (mantener el rol, NO error HTTP)
            return Ok(new ChatResponse
            {
                SessionId = sessionId,
                Response = guardResult.RejectionReason
                    ?? "Solo puedo ayudarle con la gestión de citas. ¿Desea agendar una cita?"
            });
        }

        // ═══ PASO 2: Procesar mensaje con el agente ═══
        var providers = await _adapter.GetAllProvidersAsync();
        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(_tenantContext.CurrentTenant!, providers);
        var history = await _sessionRepository.GetChatHistoryAsync(sessionId, systemPrompt);

        var response = await _agent.RespondAsync(request.Message, history);

        await _sessionRepository.UpdateChatHistoryAsync(sessionId, history);

        // ═══ PASO 3: Output Filter - Filtrar PII y prompt leaks ═══
        var filterResult = await _outputFilter.FilterAsync(response, tenantId);

        if (filterResult.WasModified)
        {
            await _auditLogger.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                SessionId = sessionId,
                EventType = "OutputFiltered",
                Content = "Respuesta filtrada por seguridad",
                Metadata = new()
                {
                    ["redactedItems"] = string.Join(", ", filterResult.RedactedItems),
                    ["originalLength"] = response.Length.ToString()
                }
            });
        }

        // Log de la respuesta del agente
        await _auditLogger.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            SessionId = sessionId,
            EventType = "AgentResponse",
            Content = filterResult.FilteredContent
        });

        return Ok(new ChatResponse
        {
            SessionId = sessionId,
            Response = filterResult.FilteredContent
        });
    }
}

public class ChatRequest
{
    public Guid SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public Guid SessionId { get; set; }
    public string Response { get; set; } = string.Empty;
}

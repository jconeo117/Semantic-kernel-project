using Microsoft.AspNetCore.Mvc;
using ReceptionistAgent.AI.Agents;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Repositories;
using ReceptionistAgent.Core.Security;
using ReceptionistAgent.Core.Services;
using System.Security.Cryptography;
using System.Text;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;
using Microsoft.AspNetCore.RateLimiting;

namespace ReceptionistAgent.Api.Controllers;

[ApiController]
[Route("api/twilio/{tenantId}")]
[EnableRateLimiting("Global")]
public class TwilioWebhookController : TwilioController
{
    private readonly IRecepcionistAgent _agent;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly TenantContext _tenantContext;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IClientDataAdapter _adapter;
    private readonly IInputGuard _inputGuard;
    private readonly IOutputFilter _outputFilter;
    private readonly IAuditLogger _auditLogger;

    public TwilioWebhookController(
        IRecepcionistAgent agent,
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
    [Consumes("application/x-www-form-urlencoded")]
    [ValidateRequest] // Filtro de seguridad de Twilio que lee Request.Headers["X-Twilio-Signature"]
    public async Task<TwiMLResult> Webhook([FromRoute] string tenantId, [FromForm] SmsRequest request)
    {
        if (!_tenantContext.IsResolved)
        {
            return TwiMLMessage("Lo siento, no puedo procesar la solicitud en este momento (Tenant no encontrado).");
        }

        var message = request.Body?.Trim() ?? string.Empty;
        var phone = request.From?.Trim() ?? string.Empty; // Twilio "From" (ej: whatsapp:+123456789)

        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(phone))
        {
            return TwiMLMessage("Mensaje inválido.");
        }

        // Mapeo determinístico: Teléfono -> Guid de SessionId
        var sessionId = GenerateSessionIdFromPhone(phone);

        // ═══ PASO 1: Input Guard ═══
        var guardResult = await _inputGuard.AnalyzeAsync(message);

        await _auditLogger.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            SessionId = sessionId,
            EventType = "WhatsAppUserMessage",
            Content = message,
            ThreatLevel = guardResult.Level,
            Metadata = new() { ["phone"] = phone }
        });

        if (!guardResult.IsAllowed)
        {
            await _auditLogger.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                SessionId = sessionId,
                EventType = "SecurityBlock",
                Content = guardResult.RejectionReason ?? "Mensaje bloqueado",
                ThreatLevel = guardResult.Level,
                Metadata = new() { ["phone"] = phone, ["originalMessage"] = message }
            });

            return TwiMLMessage(guardResult.RejectionReason ?? "Solo puedo ayudarle con la gestión de citas. ¿Desea agendar una cita?");
        }

        // ═══ PASO 2: Procesar con el Agente ═══
        var providers = await _adapter.GetAllProvidersAsync();
        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(_tenantContext.CurrentTenant!, providers);
        var history = await _sessionRepository.GetChatHistoryAsync(sessionId, systemPrompt);

        var response = await _agent.RespondAsync(message, history);

        await _sessionRepository.UpdateChatHistoryAsync(sessionId, history);

        // ═══ PASO 3: Output Filter ═══
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

        await _auditLogger.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            SessionId = sessionId,
            EventType = "WhatsAppAgentResponse",
            Content = filterResult.FilteredContent,
            Metadata = new() { ["phone"] = phone }
        });

        return TwiMLMessage(filterResult.FilteredContent);
    }

    private TwiMLResult TwiMLMessage(string message)
    {
        var response = new MessagingResponse();
        response.Message(message);
        return TwiML(response);
    }

    /// <summary>
    /// Convierte un número de teléfono de forma determinística en un Guid.
    /// Así el mismo número de teléfono siempre genera el mismo SessionId
    /// para mantener el historial de chat con el usuario.
    /// </summary>
    private static Guid GenerateSessionIdFromPhone(string phone)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("WhatsAppSessionSalt_" + phone));
        return new Guid(hash);
    }
}

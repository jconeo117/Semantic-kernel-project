using Microsoft.AspNetCore.Mvc;
using ReceptionistAgent.AI.Agents;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Connectors.Repositories;
using ReceptionistAgent.Connectors.Security;
using ReceptionistAgent.Api.Security;
using ReceptionistAgent.Core.Security;
using ReceptionistAgent.Api.Services;
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
    private readonly IChatOrchestrator _orchestrator;
    private readonly TenantContext _tenantContext;

    public TwilioWebhookController(
        IChatOrchestrator orchestrator,
        TenantContext tenantContext)
    {
        _orchestrator = orchestrator;
        _tenantContext = tenantContext;
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

        // ═══ Ejecutar pipeline mediante el orquestador ═══
        var result = await _orchestrator.ProcessMessageAsync(
            message: message,
            sessionId: sessionId,
            tenantId: tenantId,
            eventTypePrefix: "WhatsApp",
            additionalMetadata: new Dictionary<string, string> { ["phone"] = phone }
        );

        return TwiMLMessage(result.Response);
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

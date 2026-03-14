using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using ReceptionistAgent.Api.Services;
using ReceptionistAgent.Connectors.Repositories;
using ReceptionistAgent.Core.Services;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Tenant;

namespace ReceptionistAgent.Api.Controllers;

[ApiController]
[Route("api/webhook/meta")]
public class MetaWebhookController : ControllerBase
{
    private readonly ILogger<MetaWebhookController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _verifyToken;

    public MetaWebhookController(
        ILogger<MetaWebhookController> logger,
        IConfiguration config,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _verifyToken = config["Meta:VerifyToken"] ?? "agente_secreto_2026";
    }

    /// <summary>
    /// Meta verifica el webhook con un GET al registrarlo en el Developer Portal.
    /// </summary>
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        _logger.LogInformation("Meta webhook verification. Mode: {Mode}", mode);

        if (mode == "subscribe" && token == _verifyToken)
        {
            _logger.LogInformation("Meta webhook verified successfully.");
            return Ok(challenge);
        }

        _logger.LogWarning("Meta webhook verification failed. Token mismatch.");
        return Forbid();
    }

    /// <summary>
    /// Recibe mensajes entrantes de WhatsApp Cloud API.
    /// Meta espera un 200 OK inmediato — el procesamiento ocurre en background.
    /// </summary>
    [HttpPost]
    public IActionResult ReceiveMessage([FromBody] JsonElement body)
    {
        try
        {
            if (body.TryGetProperty("object", out var objProp) &&
                objProp.GetString() == "whatsapp_business_account")
            {
                _ = Task.Run(async () => await ProcessPayloadAsync(body));
                return Ok();
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Meta webhook payload.");
            return StatusCode(500);
        }
    }

    // ─── Procesamiento en background ─────────────────────────────────────────

    private async Task ProcessPayloadAsync(JsonElement body)
    {
        try
        {
            var entries = body.GetProperty("entry").EnumerateArray();
            foreach (var entry in entries)
            {
                foreach (var change in entry.GetProperty("changes").EnumerateArray())
                {
                    var value = change.GetProperty("value");
                    if (!value.TryGetProperty("messages", out var messages)) continue;

                    // El phone_number_id identifica a qué número de negocio llegó el mensaje
                    var phoneNumberId = value
                        .GetProperty("metadata")
                        .GetProperty("phone_number_id")
                        .GetString();

                    foreach (var message in messages.EnumerateArray())
                    {
                        if (!message.TryGetProperty("type", out var typeProp) ||
                            typeProp.GetString() != "text") continue;

                        var fromPhone = message.GetProperty("from").GetString();
                        var text = message.GetProperty("text").GetProperty("body").GetString();

                        if (string.IsNullOrEmpty(fromPhone) ||
                            string.IsNullOrEmpty(text) ||
                            string.IsNullOrEmpty(phoneNumberId)) continue;

                        _logger.LogInformation(
                            "Meta message from {From} via PhoneId {PhoneId}: {Text}",
                            fromPhone, phoneNumberId, text);

                        await OrchestrateAsync(fromPhone, text, phoneNumberId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background error processing Meta payload.");
        }
    }

    private async Task OrchestrateAsync(string fromPhone, string text, string phoneNumberId)
    {
        using var scope = _scopeFactory.CreateScope();

        // PASO 1: resolver tenant y asignar al TenantContext ANTES de resolver
        // cualquier servicio que dependa de él (IClientDataAdapter, IChatOrchestrator, etc.)
        var tenantResolver = scope.ServiceProvider.GetRequiredService<ITenantResolver>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();

        TenantConfiguration? tenant = null;

        if (tenantResolver is SqlTenantRepository sqlRepo)
        {
            tenant = await sqlRepo.ResolveByMetaPhoneNumberIdAsync(phoneNumberId);
        }
        else
        {
            var all = await tenantResolver.GetAllTenantsAsync();
            tenant = all.FirstOrDefault(t =>
                t.MessageProvider.Equals("Meta", StringComparison.OrdinalIgnoreCase) &&
                t.MessageProviderAccount == phoneNumberId);
        }

        if (tenant == null)
        {
            _logger.LogError(
                "No tenant found for Meta PhoneNumberId {PhoneId}. " +
                "Verify that MessageProviderAccount is set correctly in the DB.", phoneNumberId);
            return;
        }

        // PASO 2: tenant en contexto — ahora el DI puede construir IClientDataAdapter
        tenantContext.CurrentTenant = tenant;

        // PASO 3: recién ahora resolver servicios que dependen del TenantContext
        var orchestrator = scope.ServiceProvider.GetRequiredService<IChatOrchestrator>();
        var messageFactory = scope.ServiceProvider.GetRequiredService<IMessageSenderFactory>();

        var sessionId = GenerateSessionId(tenant.TenantId, fromPhone);
        var metadata = new Dictionary<string, string> { { "phone", fromPhone } };

        var result = await orchestrator.ProcessMessageAsync(
            message: text,
            sessionId: sessionId,
            tenantId: tenant.TenantId,
            eventTypePrefix: "Meta",
            additionalMetadata: metadata);

        var sender = messageFactory.CreateSender(tenant);
        await sender.SendAsync(fromPhone, result.Response);

        _logger.LogInformation(
            "Meta response sent to {To} (tenant: {TenantId})", fromPhone, tenant.TenantId);
    }

    private static Guid GenerateSessionId(string tenantId, string phone)
    {
        var input = $"MetaSessionSalt_{tenantId}_{phone}";
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReceptionistAgent.Core.Services;

namespace ReceptionistAgent.Connectors.Messaging;

/// <summary>
/// Envía mensajes outbound por la Meta Cloud API oficial de WhatsApp Business.
/// Graph API v22.0 — números en formato E.164 con prefijo +
/// </summary>
public class MetaWhatsAppSender : IMessageSender
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly string _phoneNumberId;
    private readonly string _accessToken;

    // Versión de la Graph API. Actualizar cuando Meta deprece la versión actual.
    private const string GraphApiVersion = "v22.0";

    public MetaWhatsAppSender(HttpClient httpClient, string phoneNumberId, string accessToken, ILogger logger)
    {
        _httpClient = httpClient;
        _phoneNumberId = phoneNumberId;
        _accessToken = accessToken;
        _logger = logger;
    }

    public async Task<bool> SendAsync(string to, string message)
    {
        try
        {
            // Normalizar a E.164 con prefijo + (requerido por Meta Cloud API)
            // Twilio prefija con "whatsapp:", Meta no lo usa
            var cleanPhone = to
                .Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (!cleanPhone.StartsWith("+"))
                cleanPhone = "+" + cleanPhone;

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to = cleanPhone,
                type = "text",
                text = new { body = message }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/{GraphApiVersion}/{_phoneNumberId}/messages")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Meta message sent to {To}. PhoneId: {PhoneId}", cleanPhone, _phoneNumberId);
                return true;
            }

            _logger.LogError(
                "Meta message failed. To: {To}, Status: {Status}, Body: {Body}",
                cleanPhone, response.StatusCode, responseBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending Meta message to {To}", to);
            return false;
        }
    }
}
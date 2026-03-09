using Microsoft.Extensions.Logging;
using ReceptionistAgent.Core.Services;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ReceptionistAgent.Connectors.Messaging;

/// <summary>
/// Envía mensajes outbound por Twilio API (WhatsApp).
/// Usado para recordatorios automáticos de citas.
/// </summary>
public class TwilioMessageSender : IMessageSender
{
    private readonly ILogger<TwilioMessageSender> _logger;
    private readonly string _fromNumber;

    public TwilioMessageSender(string accountSid, string authToken, string fromNumber, ILogger<TwilioMessageSender> logger)
    {
        _logger = logger;
        _fromNumber = fromNumber;

        TwilioClient.Init(accountSid, authToken);
    }

    public async Task<bool> SendAsync(string to, string message)
    {
        try
        {
            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_fromNumber),
                to: new PhoneNumber($"whatsapp:{to}")
            );

            _logger.LogInformation(
                "Twilio message sent: SID={Sid}, To={To}, Status={Status}",
                messageResource.Sid, to, messageResource.Status);

            return messageResource.Status != MessageResource.StatusEnum.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Twilio message to {To}", to);
            return false;
        }
    }
}

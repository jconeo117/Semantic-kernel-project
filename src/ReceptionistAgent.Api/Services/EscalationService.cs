using ReceptionistAgent.AI.Services;
using ReceptionistAgent.Core.Session;

namespace ReceptionistAgent.Api.Services;

public class EscalationService : IEscalationService
{
    private readonly IChatSessionRepository _chatSessionRepository;

    public EscalationService(IChatSessionRepository chatSessionRepository)
    {
        _chatSessionRepository = chatSessionRepository;
    }

    public async Task EscalateSessionAsync(Guid sessionId, string tenantId, string reason)
    {
        // Here we could also log the 'reason', send an email to the clinic, or push an alert.
        // For now, we simply flag the session in the database so it appears in the Client Dashboard Inbox.
        await _chatSessionRepository.SetNeedsHumanAttentionAsync(sessionId, tenantId, true);
    }
}

using ReceptionistAgent.AI.Agents;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Connectors.Repositories;
using ReceptionistAgent.Connectors.Security;
using ReceptionistAgent.Core.Security;
using ReceptionistAgent.Core.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReceptionistAgent.Api.Services;

public class ChatOrchestrator : IChatOrchestrator
{
    private readonly IRecepcionistAgent _agent;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IClientDataAdapter _adapter;
    private readonly IInputGuard _inputGuard;
    private readonly IOutputFilter _outputFilter;
    private readonly IAuditLogger _auditLogger;
    private readonly TenantContext _tenantContext;

    public ChatOrchestrator(
        IRecepcionistAgent agent,
        IChatSessionRepository sessionRepository,
        IPromptBuilder promptBuilder,
        IClientDataAdapter adapter,
        IInputGuard inputGuard,
        IOutputFilter outputFilter,
        IAuditLogger auditLogger,
        TenantContext tenantContext)
    {
        _agent = agent;
        _sessionRepository = sessionRepository;
        _promptBuilder = promptBuilder;
        _adapter = adapter;
        _inputGuard = inputGuard;
        _outputFilter = outputFilter;
        _auditLogger = auditLogger;
        _tenantContext = tenantContext;
    }

    public async Task<OrchestrationResult> ProcessMessageAsync(
        string message,
        Guid sessionId,
        string tenantId,
        string eventTypePrefix,
        Dictionary<string, string>? additionalMetadata = null)
    {
        var metadata = additionalMetadata ?? new Dictionary<string, string>();

        // ═══ PASO 1: Input Guard ═══
        var guardResult = await _inputGuard.AnalyzeAsync(message);

        await _auditLogger.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            SessionId = sessionId,
            EventType = $"{eventTypePrefix}UserMessage",
            Content = message,
            ThreatLevel = guardResult.Level,
            Metadata = metadata
        });

        if (!guardResult.IsAllowed)
        {
            metadata["originalMessage"] = message;

            await _auditLogger.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                SessionId = sessionId,
                EventType = "SecurityBlock",
                Content = guardResult.RejectionReason ?? "Mensaje bloqueado",
                ThreatLevel = guardResult.Level,
                Metadata = metadata
            });

            return new OrchestrationResult
            {
                Response = guardResult.RejectionReason ?? "Solo puedo ayudarle con la gestión de citas. ¿Desea agendar una cita?",
                WasFiltered = true
            };
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
            var filterMetadata = new Dictionary<string, string>(metadata)
            {
                ["redactedItems"] = string.Join(", ", filterResult.RedactedItems),
                ["originalLength"] = response.Length.ToString()
            };

            await _auditLogger.LogAsync(new AuditEntry
            {
                TenantId = tenantId,
                SessionId = sessionId,
                EventType = "OutputFiltered",
                Content = "Respuesta filtrada por seguridad",
                Metadata = filterMetadata
            });
        }

        await _auditLogger.LogAsync(new AuditEntry
        {
            TenantId = tenantId,
            SessionId = sessionId,
            EventType = $"{eventTypePrefix}AgentResponse",
            Content = filterResult.FilteredContent,
            Metadata = metadata
        });

        return new OrchestrationResult
        {
            Response = filterResult.FilteredContent,
            WasFiltered = filterResult.WasModified,
            RedactedItems = filterResult.RedactedItems
        };
    }
}

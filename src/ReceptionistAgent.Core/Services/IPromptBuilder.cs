using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.Core.Services;

/// <summary>
/// Genera el system prompt del agente dinámicamente,
/// basándose en la configuración del tenant actual.
/// </summary>
public interface IPromptBuilder
{
    Task<string> BuildSystemPromptAsync(TenantConfiguration tenant, List<ServiceProvider> providers);
}

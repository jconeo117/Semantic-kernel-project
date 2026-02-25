using ClinicSimulator.Core.Models;

namespace ClinicSimulator.Core.Services;

/// <summary>
/// Genera el system prompt del agente dinámicamente,
/// basándose en la configuración del tenant actual.
/// </summary>
public interface IPromptBuilder
{
    Task<string> BuildSystemPromptAsync(TenantConfiguration tenant, List<ServiceProvider> providers);
}

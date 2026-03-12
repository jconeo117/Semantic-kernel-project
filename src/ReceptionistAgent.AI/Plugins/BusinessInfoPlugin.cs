using System.ComponentModel;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Models;
using Microsoft.SemanticKernel;

namespace ReceptionistAgent.AI.Plugins;

public class BusinessInfoPlugin
{
    private readonly IClientDataAdapter _adapter;
    private readonly TenantContext _tenantContext;

    public BusinessInfoPlugin(IClientDataAdapter adapter, TenantContext tenantContext)
    {
        _adapter = adapter;
        _tenantContext = tenantContext;
    }

    [KernelFunction]
    [Description("Proporciona información sobre los proveedores de servicio disponibles")]
    public async Task<string> GetProviderInfo(
        [Description("Rol/especialidad buscada o 'todos' para listar todos")] string query = "todos")
    {
        List<ServiceProvider> providers;

        if (query.Equals("todos", StringComparison.OrdinalIgnoreCase))
        {
            providers = await _adapter.GetAllProvidersAsync();
        }
        else
        {
            providers = await _adapter.SearchProvidersAsync(query);
        }

        if (!providers.Any())
            return $"No se encontraron proveedores para '{query}'";

        return "Proveedores disponibles:\n" +
               string.Join("\n", providers.Select(p => $"- {p.Name} ({p.Role})"));
    }

}

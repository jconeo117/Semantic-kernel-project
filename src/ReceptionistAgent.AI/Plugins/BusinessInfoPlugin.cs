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

    [KernelFunction]
    [Description("Información sobre ubicación, horarios y servicios del negocio")]
    public string GetBusinessInfo(
        [Description("Tipo: ubicacion, horarios, servicios, seguros, precios")] string infoType)
    {
        var tenant = _tenantContext.CurrentTenant;

        if (tenant == null)
            return "Información del negocio no disponible.";

        return infoType.ToLower() switch
        {
            "ubicacion" => $"{tenant.BusinessName}\n{tenant.Address}\nTeléfono: {tenant.Phone}",

            "horarios" => $"Horarios de atención:\n{tenant.WorkingHours}",

            "servicios" => tenant.Services.Any()
                ? $"Servicios disponibles:\n{string.Join("\n", tenant.Services.Select(s => $"- {s}"))}"
                : "Consultar servicios disponibles con el negocio.",

            "seguros" => tenant.AcceptedInsurance.Any()
                ? $"Seguros aceptados:\n{string.Join("\n", tenant.AcceptedInsurance.Select(i => $"- {i}"))}"
                : "No aplica o consultar con el negocio.",

            "precios" => tenant.Pricing.Any()
                ? string.Join("\n", tenant.Pricing.Select(p => $"- {p.Key}: {p.Value}"))
                : "Consultar precios en el establecimiento.",

            _ => "Tipos de información disponibles: ubicacion, horarios, servicios, seguros, precios"
        };
    }
}

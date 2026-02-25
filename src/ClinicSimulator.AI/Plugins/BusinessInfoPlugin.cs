using System.ComponentModel;
using ClinicSimulator.Core.Adapters;
using ClinicSimulator.Core.Models;
using Microsoft.SemanticKernel;

namespace ClinicSimulator.AI.Plugins;

public class BusinessInfoPlugin
{
    private readonly IClientDataAdapter _adapter;

    public BusinessInfoPlugin(IClientDataAdapter adapter)
    {
        _adapter = adapter;
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
        // TODO: En Phase 2 (Multi-Tenant), estos datos vendrán de TenantConfiguration.
        // Por ahora mantenemos datos demo para que la API siga funcionando.
        return infoType.ToLower() switch
        {
            "ubicacion" => "Clínica Vista Clara\nAv. Principal 123, Montería\nEntre calles 5 y 6",
            "horarios" => "Horarios de atención:\nLunes a Viernes: 9:00 AM - 6:00 PM\nSábados: 9:00 AM - 1:00 PM",
            "servicios" => "Servicios disponibles:\n- Consulta general oftalmológica\n- Cirugía refractiva\n- Tratamiento de glaucoma\n- Enfermedades de la retina\n- Exámenes de la vista",
            "seguros" => "Seguros aceptados:\n- Pacífico\n- Rímac\n- Mapfre\n- Nueva EPS",
            "precios" => "Consulta general: $50 USD\nConsulta especializada: $80 USD\n(Precios pueden variar según el seguro)",
            _ => "Tipos de información disponibles: ubicacion, horarios, servicios, seguros, precios"
        };
    }
}

using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace ClinicSimulator.AI.Plugins;

public class ClinicInfoPlugin
{
    [KernelFunction]
    [Description("Proporciona información sobre los doctores disponibles")]
    public string GetDoctorInfo(
        [Description("Especialidad buscada o 'todos' para listar todos")] string query = "todos")
    {
        var doctors = new[]
        {
            new { Id = "DR001", Name = "Dr. Carlos Ramírez", Specialty = "Oftalmología General" },
            new { Id = "DR002", Name = "Dra. María González", Specialty = "Retina" }
        };

        if (query.ToLower() == "todos")
        {
            return "Doctores disponibles:\n" +
                   string.Join("\n", doctors.Select(d => $"- {d.Name} ({d.Specialty})"));
        }

        var filtered = doctors.Where(d =>
            d.Specialty.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            d.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (!filtered.Any())
            return $"No se encontraron doctores para '{query}'";

        return string.Join("\n", filtered.Select(d => $"- {d.Name} ({d.Specialty})"));
    }

    [KernelFunction]
    [Description("Información sobre ubicación, horarios y servicios de la clínica")]
    public string GetClinicInfo(
        [Description("Tipo: ubicacion, horarios, servicios, seguros, precios")] string infoType)
    {
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
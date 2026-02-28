using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.Core.Adapters;

/// <summary>
/// Factory para crear IClientDataAdapter a partir de TenantConfiguration.
/// Convierte TenantProviderConfig → ServiceProvider y crea InMemoryClientAdapter.
/// 
/// En el futuro, esta factory podría crear adapters de diferentes tipos
/// (SQL, PostgreSQL, REST API) según la configuración del tenant.
/// </summary>
public class ClientDataAdapterFactory
{
    public IClientDataAdapter CreateAdapter(TenantConfiguration tenantConfig)
    {
        var providers = tenantConfig.Providers.Select(p => new ServiceProvider
        {
            Id = p.Id,
            TenantId = tenantConfig.TenantId,
            Name = p.Name,
            Role = p.Role,
            WorkingDays = p.WorkingDays
                .Select(d => Enum.TryParse<DayOfWeek>(d, true, out var day) ? day : (DayOfWeek?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList(),
            StartTime = TimeSpan.TryParse(p.StartTime, out var start) ? start : new TimeSpan(9, 0, 0),
            EndTime = TimeSpan.TryParse(p.EndTime, out var end) ? end : new TimeSpan(18, 0, 0),
            SlotDurationMinutes = p.SlotDurationMinutes
        }).ToList();

        return new InMemoryClientAdapter(providers);
    }
}

using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Services;
using Microsoft.Extensions.Logging;

namespace ReceptionistAgent.Connectors.Adapters;

/// <summary>
/// Factory para crear IClientDataAdapter a partir de TenantConfiguration.
/// Convierte TenantProviderConfig → ServiceProvider y crea InMemoryClientAdapter.
/// 
/// En el futuro, esta factory podría crear adapters de diferentes tipos
/// (SQL, PostgreSQL, REST API) según la configuración del tenant.
/// </summary>
public class ClientDataAdapterFactory
{
    private readonly IBookingBackupService _backupService;
    private readonly ILoggerFactory _loggerFactory;

    public ClientDataAdapterFactory(IBookingBackupService backupService, ILoggerFactory loggerFactory)
    {
        _backupService = backupService;
        _loggerFactory = loggerFactory;
    }

    public IClientDataAdapter CreateAdapter(TenantConfiguration tenantConfig)
    {
        if (tenantConfig.DbType.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(tenantConfig.ConnectionString))
                throw new InvalidOperationException($"Tenant '{tenantConfig.TenantId}' specified PostgreSql but no ConnectionString was found.");

            return new PostgreSqlClientDataAdapter(tenantConfig.ConnectionString, _backupService, _loggerFactory.CreateLogger<PostgreSqlClientDataAdapter>());
        }

        if (tenantConfig.DbType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(tenantConfig.ConnectionString))
                throw new InvalidOperationException($"Tenant '{tenantConfig.TenantId}' specified SqlServer but no ConnectionString was found.");

            return new SqlClientDataAdapter(tenantConfig.ConnectionString, _backupService, _loggerFactory.CreateLogger<SqlClientDataAdapter>());
        }

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

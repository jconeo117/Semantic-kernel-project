using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.Core.Tenant;

/// <summary>
/// Resuelve la configuración de un tenant a partir de su ID.
/// Diseñado para ser extensible: la implementación puede leer
/// de appsettings, base de datos, API externa, etc.
/// </summary>
public interface ITenantResolver
{
    Task<TenantConfiguration?> ResolveAsync(string tenantId);
    Task<List<string>> GetAllTenantIdsAsync();
}

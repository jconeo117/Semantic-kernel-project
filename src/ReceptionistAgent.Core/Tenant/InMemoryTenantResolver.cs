using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.Core.Tenant;

/// <summary>
/// Implementación en memoria de ITenantResolver.
/// Carga configuraciones de tenants desde un diccionario inyectado por constructor.
/// 
/// Para migrar a base de datos, simplemente crear una nueva implementación
/// de ITenantResolver que lea de EF Core, Dapper, etc.
/// </summary>
public class InMemoryTenantResolver : ITenantResolver
{
    private readonly Dictionary<string, TenantConfiguration> _tenants;

    public InMemoryTenantResolver(Dictionary<string, TenantConfiguration> tenants)
    {
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
    }

    public Task<TenantConfiguration?> ResolveAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            return Task.FromResult<TenantConfiguration?>(null);

        // Búsqueda case-insensitive para mayor flexibilidad
        var key = _tenants.Keys.FirstOrDefault(k =>
            k.Equals(tenantId, StringComparison.OrdinalIgnoreCase));

        if (key == null)
            return Task.FromResult<TenantConfiguration?>(null);

        return Task.FromResult<TenantConfiguration?>(_tenants[key]);
    }

    public Task<List<string>> GetAllTenantIdsAsync()
    {
        return Task.FromResult(_tenants.Keys.ToList());
    }
}

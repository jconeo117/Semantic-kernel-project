using ClinicSimulator.Core.Adapters;

namespace ClinicSimulator.Connectors;

/// <summary>
/// Implementación de IConnectorFactory que crea adapters SQL Server
/// usando Dapper para acceso a datos.
/// </summary>
public class ConnectorFactory : IConnectorFactory
{
    /// <inheritdoc />
    public IClientDataAdapter CreateConnector(string tenantId, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("El tenantId no puede estar vacío.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("La connectionString no puede estar vacía.", nameof(connectionString));

        // TODO: Crear e inyectar el SqlClientDataAdapter cuando esté implementado
        throw new NotImplementedException(
            $"SqlClientDataAdapter para tenant '{tenantId}' será implementado en las siguientes tasks.");
    }
}

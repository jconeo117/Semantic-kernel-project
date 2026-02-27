using ClinicSimulator.Core.Adapters;

namespace ClinicSimulator.Connectors;

/// <summary>
/// Factory para crear instancias de IClientDataAdapter conectadas a bases de datos externas.
/// Cada tenant puede tener su propia conexión y mapeo de campos.
/// </summary>
public interface IConnectorFactory
{
    /// <summary>
    /// Crea un adapter que conecta con la base de datos del tenant especificado.
    /// </summary>
    /// <param name="tenantId">Identificador del tenant.</param>
    /// <param name="connectionString">Cadena de conexión a la base de datos.</param>
    /// <returns>Instancia de IClientDataAdapter conectada al origen de datos.</returns>
    IClientDataAdapter CreateConnector(string tenantId, string connectionString);
}

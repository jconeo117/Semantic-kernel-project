namespace ClinicSimulator.Connectors.Models;

/// <summary>
/// Define el tipo de conector/adapter que se utilizará para acceder
/// a los datos del tenant.
/// </summary>
public enum ConnectorType
{
    /// <summary>
    /// Datos almacenados en memoria. Ideal para testing y demos.
    /// No requiere connection string.
    /// </summary>
    InMemory = 0,

    /// <summary>
    /// Conexión directa a SQL Server usando Dapper.
    /// Requiere connection string y field mappings configurados.
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// Reservado para futuro: conexión vía REST API externa.
    /// </summary>
    RestApi = 2
}

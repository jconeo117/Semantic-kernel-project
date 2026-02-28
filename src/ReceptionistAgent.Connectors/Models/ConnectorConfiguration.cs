namespace ReceptionistAgent.Connectors.Models;

/// <summary>
/// Configuración de conexión para un tenant específico.
/// Define cómo el sistema se conecta al origen de datos del cliente.
/// </summary>
public class ConnectorConfiguration
{
    /// <summary>
    /// Tipo de conector a utilizar. Determina qué adapter se instanciará.
    /// Default: InMemory.
    /// </summary>
    public ConnectorType Type { get; set; } = ConnectorType.InMemory;

    /// <summary>
    /// Cadena de conexión a la base de datos.
    /// Requerido cuando Type == SqlServer.
    /// Ejemplo: "Server=localhost;Database=ClinicaDB;Trusted_Connection=true;"
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Configuraciones adicionales específicas del conector.
    /// Ejemplos: CommandTimeout, MaxRetries, etc.
    /// </summary>
    public Dictionary<string, string>? Settings { get; set; }

    /// <summary>
    /// Mapeo de campos del dominio a columnas SQL del tenant.
    /// Requerido cuando Type == SqlServer.
    /// </summary>
    public FieldMappingConfig? FieldMappings { get; set; }

    /// <summary>
    /// Valida que la configuración tenga los campos requeridos
    /// según el tipo de conector seleccionado.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si faltan campos requeridos para el tipo de conector.
    /// </exception>
    public void Validate()
    {
        if (Type == ConnectorType.SqlServer)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
                throw new InvalidOperationException(
                    "ConnectionString es requerido cuando el tipo de conector es SqlServer.");

            if (FieldMappings == null)
                throw new InvalidOperationException(
                    "FieldMappings es requerido cuando el tipo de conector es SqlServer.");

            FieldMappings.Validate();
        }
    }
}

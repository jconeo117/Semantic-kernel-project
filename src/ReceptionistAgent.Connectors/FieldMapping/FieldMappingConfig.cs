namespace ReceptionistAgent.Connectors.Models;

/// <summary>
/// Configuración de mapeo de campos entre el modelo de dominio
/// y las columnas/tablas de la base de datos del tenant.
/// Permite que cada cliente use su propio esquema SQL sin cambios en el código.
/// </summary>
public class FieldMappingConfig
{
    /// <summary>
    /// Nombre de la tabla de reservas/citas en la BD del tenant.
    /// Ejemplo: "dbo.citas", "appointments", "reservas"
    /// </summary>
    public string BookingsTableName { get; set; } = "bookings";

    /// <summary>
    /// Nombre de la tabla de proveedores de servicio en la BD del tenant.
    /// Ejemplo: "dbo.doctores", "providers", "empleados"
    /// </summary>
    public string ProvidersTableName { get; set; } = "providers";

    /// <summary>
    /// Mapeo de propiedades de BookingRecord a columnas SQL.
    /// Key: nombre de la propiedad del dominio (ej: "ClientName")
    /// Value: nombre de la columna SQL (ej: "nombre_paciente")
    /// 
    /// Propiedades mapeables:
    /// Id, TenantId, ConfirmationCode, ClientName, ProviderId,
    /// ProviderName, ScheduledDate, ScheduledTime, Status,
    /// CreatedAt, UpdatedAt
    /// </summary>
    public Dictionary<string, string> BookingFieldMappings { get; set; } = new()
    {
        ["Id"] = "id",
        ["TenantId"] = "tenant_id",
        ["ConfirmationCode"] = "confirmation_code",
        ["ClientName"] = "client_name",
        ["ProviderId"] = "provider_id",
        ["ProviderName"] = "provider_name",
        ["ScheduledDate"] = "scheduled_date",
        ["ScheduledTime"] = "scheduled_time",
        ["Status"] = "status",
        ["CreatedAt"] = "created_at",
        ["UpdatedAt"] = "updated_at"
    };

    /// <summary>
    /// Mapeo de propiedades de ServiceProvider a columnas SQL.
    /// Key: nombre de la propiedad del dominio (ej: "Name")
    /// Value: nombre de la columna SQL (ej: "nombre")
    /// 
    /// Propiedades mapeables:
    /// Id, TenantId, Name, Role, WorkingDays, StartTime, EndTime,
    /// SlotDurationMinutes, IsAvailable
    /// </summary>
    public Dictionary<string, string> ProviderFieldMappings { get; set; } = new()
    {
        ["Id"] = "id",
        ["TenantId"] = "tenant_id",
        ["Name"] = "name",
        ["Role"] = "role",
        ["WorkingDays"] = "working_days",
        ["StartTime"] = "start_time",
        ["EndTime"] = "end_time",
        ["SlotDurationMinutes"] = "slot_duration_minutes",
        ["IsAvailable"] = "is_available"
    };

    /// <summary>
    /// Mapeo de campos personalizados (CustomFields) del tenant.
    /// Key: nombre lógico del campo (ej: "clientId", "phone", "email")
    /// Value: nombre de la columna SQL (ej: "documento_paciente", "telefono")
    /// 
    /// Estos campos se almacenan en BookingRecord.CustomFields
    /// y se leen/escriben dinámicamente según este mapeo.
    /// </summary>
    public Dictionary<string, string> CustomFieldMappings { get; set; } = new();

    /// <summary>
    /// Valida que los campos mínimos requeridos estén mapeados.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si faltan mapeos obligatorios.
    /// </exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BookingsTableName))
            throw new InvalidOperationException("BookingsTableName es requerido.");

        if (string.IsNullOrWhiteSpace(ProvidersTableName))
            throw new InvalidOperationException("ProvidersTableName es requerido.");

        var requiredBookingFields = new[] { "Id", "ClientName", "ProviderId", "ScheduledDate", "ScheduledTime", "Status" };
        foreach (var field in requiredBookingFields)
        {
            if (!BookingFieldMappings.ContainsKey(field))
                throw new InvalidOperationException(
                    $"BookingFieldMappings debe contener el campo requerido '{field}'.");
        }

        var requiredProviderFields = new[] { "Id", "Name", "Role" };
        foreach (var field in requiredProviderFields)
        {
            if (!ProviderFieldMappings.ContainsKey(field))
                throw new InvalidOperationException(
                    $"ProviderFieldMappings debe contener el campo requerido '{field}'.");
        }
    }
}

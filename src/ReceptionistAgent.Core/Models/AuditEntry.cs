namespace ReceptionistAgent.Core.Models;

/// <summary>
/// Registro de auditoría para cada interacción con el agente.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = string.Empty;
    public Guid SessionId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tipo de evento: UserMessage, AgentResponse, SecurityBlock, PluginCall, OutputFiltered
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del evento (mensaje del usuario o respuesta del agente).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Nivel de amenaza detectado (solo para eventos de seguridad).
    /// </summary>
    public Security.ThreatLevel? ThreatLevel { get; set; }

    /// <summary>
    /// Metadatos adicionales (plugin usado, duración, razón de bloqueo, etc.)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

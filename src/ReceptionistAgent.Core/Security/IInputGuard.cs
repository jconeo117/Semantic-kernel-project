namespace ReceptionistAgent.Core.Security;

/// <summary>
/// Resultado del análisis de seguridad de un mensaje de usuario.
/// </summary>
public record GuardResult(bool IsAllowed, string? RejectionReason, ThreatLevel Level);

/// <summary>
/// Nivel de amenaza detectado en un mensaje.
/// </summary>
public enum ThreatLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}

/// <summary>
/// Analiza mensajes de usuario antes de enviarlos al LLM para detectar
/// intentos de prompt injection, jailbreak, o extracción de datos.
/// </summary>
public interface IInputGuard
{
    Task<GuardResult> AnalyzeAsync(string userMessage);
}

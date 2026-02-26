namespace ClinicSimulator.Core.Session;

/// <summary>
/// Contexto de sesión que rastrea las identidades validadas del paciente
/// durante una conversación o request. Permite verificar ownership de reservas.
/// </summary>
public interface ISessionContext
{
    /// <summary>
    /// ID del paciente validado en esta sesión (documento de identidad).
    /// </summary>
    string? ValidatedPatientId { get; }

    /// <summary>
    /// Códigos de confirmación que han sido validados en esta sesión.
    /// </summary>
    HashSet<string> ValidatedConfirmationCodes { get; }

    /// <summary>
    /// Marca un ID de paciente como validado en esta sesión.
    /// </summary>
    void ValidatePatientId(string patientId);

    /// <summary>
    /// Marca un código de confirmación como validado en esta sesión.
    /// </summary>
    void ValidateConfirmationCode(string code);

    /// <summary>
    /// Verifica si un ID de paciente fue validado en esta sesión.
    /// </summary>
    bool IsPatientValidated(string patientId);

    /// <summary>
    /// Verifica si un código de confirmación fue validado en esta sesión.
    /// </summary>
    bool IsCodeValidated(string code);
}

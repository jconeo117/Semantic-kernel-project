namespace ClinicSimulator.Core.Session;

/// <summary>
/// Implementación scoped de ISessionContext.
/// Mantiene en memoria los IDs y códigos validados durante un request/sesión.
/// </summary>
public class SessionContext : ISessionContext
{
    private readonly HashSet<string> _validatedPatientIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _validatedConfirmationCodes = new(StringComparer.OrdinalIgnoreCase);

    public string? ValidatedPatientId { get; private set; }

    public HashSet<string> ValidatedConfirmationCodes => _validatedConfirmationCodes;

    public void ValidatePatientId(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            throw new ArgumentException("El ID del paciente no puede estar vacío.", nameof(patientId));

        ValidatedPatientId = patientId;
        _validatedPatientIds.Add(patientId);
    }

    public void ValidateConfirmationCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("El código de confirmación no puede estar vacío.", nameof(code));

        _validatedConfirmationCodes.Add(code);
    }

    public bool IsPatientValidated(string patientId)
    {
        return !string.IsNullOrWhiteSpace(patientId) &&
               _validatedPatientIds.Contains(patientId);
    }

    public bool IsCodeValidated(string code)
    {
        return !string.IsNullOrWhiteSpace(code) &&
               _validatedConfirmationCodes.Contains(code);
    }
}

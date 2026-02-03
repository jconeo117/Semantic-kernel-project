public class Patient
{
    // Identificación
    public Guid Id { get; set; }                        // string - ej: "PAT001" o "P001"

    // Información básica
    public string Name { get; set; }                      // string
    public string Phone { get; set; }                     // string - formato libre
    public string Email { get; set; }                     // string

    // Historial
    public DateTime? LastVisit { get; set; }              // DateTime? - nullable
    public List<string> MedicalHistory { get; set; }      // List<string> - notas

    // Clasificación
    public PatientType Type { get; set; }                 // enum

    // Metadata
    public DateTime CreatedAt { get; set; }               // DateTime
}

public enum PatientType
{
    Real = 0,      // Paciente real (humano)
    Agent = 1      // Paciente simulado (NPC)
}
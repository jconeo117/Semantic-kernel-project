namespace ClinicSimulator.Core.Models;

public class Doctor
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public List<DayOfWeek> workingDays { get; set; } = new();
    public TimeSpan startTime { get; set; }
    public TimeSpan endTime { get; set; }
    public int durationOfAppointment { get; set; } = 30;
    public bool isAvailable { get; set; }
}
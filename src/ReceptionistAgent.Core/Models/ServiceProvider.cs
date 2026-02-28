namespace ReceptionistAgent.Core.Models;

public class ServiceProvider
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;              // "Doctor", "Estilista", "Mecánico", etc.
    public List<DayOfWeek> WorkingDays { get; set; } = new();
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsAvailable { get; set; } = true;
    public Dictionary<string, object> CustomFields { get; set; } = new();
}

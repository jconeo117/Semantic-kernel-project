public class Doctor
{

    //informacion del medico
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;

    //horario de trabajo
    public List<dayOfWeek> workingDays { get; set; }
    public TimeSpan startTime { get; set; }
    public TimeSpan endTime { get; set; }

    //opcionales
    public int durationOfAppointment { get; set; } = 30;
    public bool isAvailable { get; set; }
}

public enum dayOfWeek
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}
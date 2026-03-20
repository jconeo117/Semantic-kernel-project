namespace ReceptionistAgent.Core.Models;

public class MetricsSummary
{
    public string TenantId { get; set; } = "";
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TotalMessages { get; set; }
    public int SecurityBlocks { get; set; }
    public int UniqueSessions { get; set; }
    public List<DailyCount> MessagesPerDay { get; set; } = [];
}

public class DailyCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

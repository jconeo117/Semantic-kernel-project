using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.Core.Repositories;

public interface IMetricsRepository
{
    Task<ReceptionistAgent.Core.Models.MetricsSummary> GetMetricsAsync(string? tenantId, DateTime from, DateTime to);
}

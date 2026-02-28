namespace ReceptionistAgent.Core.Models;

/// <summary>
/// Objeto scoped que lleva el tenant del request actual.
/// Es inyectado por el TenantMiddleware al inicio de cada request.
/// </summary>
public class TenantContext
{
    public TenantConfiguration? CurrentTenant { get; set; }
    public bool IsResolved => CurrentTenant != null;
}

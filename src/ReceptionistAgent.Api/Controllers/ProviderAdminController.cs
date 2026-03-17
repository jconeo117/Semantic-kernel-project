using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReceptionistAgent.Api.Security;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Tenant;

namespace ReceptionistAgent.Api.Controllers;

/// <summary>
/// CRUD de providers por tenant.
/// Protegido con API Key.
/// </summary>
[ApiController]
[Route("api/admin/tenants/{tenantId}/providers")]
[EnableRateLimiting("Global")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class ProviderAdminController : ControllerBase
{
    private readonly ITenantResolver _tenantResolver;

    public ProviderAdminController(ITenantResolver tenantResolver)
    {
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    public IActionResult GetAll(string tenantId)
    {
        return StatusCode(StatusCodes.Status410Gone, new { error = "This endpoint is deprecated. Providers are now managed directly in the client database for SQL tenants." });
    }

    [HttpGet("{providerId}")]
    public IActionResult GetById(string tenantId, string providerId)
    {
        return StatusCode(StatusCodes.Status410Gone, new { error = "This endpoint is deprecated. Providers are now managed directly in the client database for SQL tenants." });
    }

    [HttpPost]
    public IActionResult Create(string tenantId, [FromBody] TenantProviderConfig provider)
    {
        return StatusCode(StatusCodes.Status410Gone, new { error = "This endpoint is deprecated. Providers are now managed directly in the client database for SQL tenants." });
    }

    [HttpPut("{providerId}")]
    public IActionResult Update(string tenantId, string providerId, [FromBody] TenantProviderConfig provider)
    {
        return StatusCode(StatusCodes.Status410Gone, new { error = "This endpoint is deprecated. Providers are now managed directly in the client database for SQL tenants." });
    }

    [HttpDelete("{providerId}")]
    public IActionResult Delete(string tenantId, string providerId)
    {
        return StatusCode(StatusCodes.Status410Gone, new { error = "This endpoint is deprecated. Providers are now managed directly in the client database for SQL tenants." });
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ReceptionistAgent.Core.Tenant;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ReceptionistAgent.Api.Controllers;

[ApiController]
[Route("api/tenant/auth")]
public class TenantAuthController : ControllerBase
{
    private readonly ITenantResolver _tenantResolver;
    private readonly IConfiguration _config;

    public TenantAuthController(ITenantResolver tenantResolver, IConfiguration config)
    {
        _tenantResolver = tenantResolver;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and Password are required.");

        var tenant = await _tenantResolver.AuthenticateAsync(request.Username, request.Password);
        if (tenant == null)
            return Unauthorized("Invalid username or password.");

        var secretKey = _config["Jwt:Key"] ?? "SUPER_SECRET_JWT_KEY_CHANGE_ME_IN_PRODUCTION!!!!";
        var issuer = _config["Jwt:Issuer"] ?? "ReceptionistAI";
        var audience = _config["Jwt:Audience"] ?? "ReceptionistAI_ClientDashboard";

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, tenant.TenantId),
                new Claim(ClaimTypes.Name, tenant.Username ?? tenant.TenantId),
                new Claim("BusinessName", tenant.BusinessName),
                new Claim("tenant_id", tenant.TenantId)
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return Ok(new
        {
            Token = tokenString,
            TenantId = tenant.TenantId,
            BusinessName = tenant.BusinessName
        });
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

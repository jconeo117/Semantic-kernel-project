using ClinicSimulator.Core.Models;
using ClinicSimulator.Core.Tenant;
using Xunit;

namespace ClinicSimulator.Tests.Tenant;

public class TenantResolverTests
{
    private readonly InMemoryTenantResolver _resolver;

    public TenantResolverTests()
    {
        var tenants = new Dictionary<string, TenantConfiguration>
        {
            ["clinica-vista-clara"] = new TenantConfiguration
            {
                TenantId = "clinica-vista-clara",
                BusinessName = "Clínica Vista Clara",
                BusinessType = "clinic"
            },
            ["salon-bella"] = new TenantConfiguration
            {
                TenantId = "salon-bella",
                BusinessName = "Salón Bella",
                BusinessType = "salon"
            }
        };

        _resolver = new InMemoryTenantResolver(tenants);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnTenantConfig_WhenTenantExists()
    {
        var result = await _resolver.ResolveAsync("clinica-vista-clara");

        Assert.NotNull(result);
        Assert.Equal("Clínica Vista Clara", result.BusinessName);
        Assert.Equal("clinic", result.BusinessType);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnNull_WhenTenantDoesNotExist()
    {
        var result = await _resolver.ResolveAsync("tenant-fantasma");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllTenantIdsAsync_ShouldReturnAllIds()
    {
        var result = await _resolver.GetAllTenantIdsAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains("clinica-vista-clara", result);
        Assert.Contains("salon-bella", result);
    }

    [Fact]
    public async Task ResolveAsync_ShouldBeCaseInsensitive()
    {
        var result = await _resolver.ResolveAsync("CLINICA-VISTA-CLARA");

        Assert.NotNull(result);
        Assert.Equal("clinica-vista-clara", result.TenantId);
    }
}

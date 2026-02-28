using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Services;
using Xunit;

namespace ReceptionistAgent.Tests.Services;

public class PromptBuilderTests
{
    private readonly PromptBuilder _builder;

    public PromptBuilderTests()
    {
        _builder = new PromptBuilder();
    }

    private static TenantConfiguration CreateClinicTenant() => new()
    {
        TenantId = "test-clinic",
        BusinessName = "Clínica Test",
        BusinessType = "clinic",
        Address = "Calle 1, Ciudad",
        Phone = "123-456",
        WorkingHours = "Lunes a Viernes: 9-6",
        Services = ["Consulta general", "Cirugía"],
        AcceptedInsurance = ["EPS1", "EPS2"],
        Pricing = new() { ["Consulta"] = "$50" }
    };

    private static TenantConfiguration CreateSalonTenant() => new()
    {
        TenantId = "test-salon",
        BusinessName = "Salón Test",
        BusinessType = "salon",
        Address = "Av. 2, Ciudad",
        Phone = "789-012",
        WorkingHours = "Martes a Sábado: 8-7",
        Services = ["Corte", "Manicure"],
        AcceptedInsurance = [],
        Pricing = new() { ["Corte"] = "$25.000" }
    };

    private static List<ServiceProvider> CreateTestProviders() =>
    [
        new() { Id = "P1", Name = "Dr. Test Uno", Role = "General" },
        new() { Id = "P2", Name = "Dra. Test Dos", Role = "Retina" }
    ];

    [Fact]
    public async Task BuildSystemPromptAsync_ShouldIncludeBusinessName()
    {
        var tenant = CreateClinicTenant();
        var prompt = await _builder.BuildSystemPromptAsync(tenant, CreateTestProviders());

        Assert.Contains("Clínica Test", prompt);
    }

    [Fact]
    public async Task BuildSystemPromptAsync_ShouldIncludeProviders()
    {
        var tenant = CreateClinicTenant();
        var providers = CreateTestProviders();
        var prompt = await _builder.BuildSystemPromptAsync(tenant, providers);

        Assert.Contains("Dr. Test Uno", prompt);
        Assert.Contains("Dra. Test Dos", prompt);
        Assert.Contains("General", prompt);
        Assert.Contains("Retina", prompt);
    }

    [Fact]
    public async Task BuildSystemPromptAsync_ShouldIncludeCurrentDate()
    {
        var tenant = CreateClinicTenant();
        var prompt = await _builder.BuildSystemPromptAsync(tenant, CreateTestProviders());

        Assert.Contains(DateTime.UtcNow.ToString("yyyy-MM-dd"), prompt);
    }

    [Fact]
    public async Task BuildSystemPromptAsync_ShouldAdaptToClinicType()
    {
        var tenant = CreateClinicTenant();
        var prompt = await _builder.BuildSystemPromptAsync(tenant, CreateTestProviders());

        Assert.Contains("clinic", prompt);
        Assert.Contains("Consulta general", prompt);
        Assert.Contains("EPS1", prompt);
    }

    [Fact]
    public async Task BuildSystemPromptAsync_ShouldAdaptToSalonType()
    {
        var tenant = CreateSalonTenant();
        var providers = new List<ServiceProvider>
        {
            new() { Id = "S1", Name = "Ana López", Role = "Estilista" }
        };
        var prompt = await _builder.BuildSystemPromptAsync(tenant, providers);

        Assert.Contains("Salón Test", prompt);
        Assert.Contains("salon", prompt);
        Assert.Contains("Corte", prompt);
        Assert.Contains("Ana López", prompt);
        Assert.DoesNotContain("EPS", prompt);
    }
}

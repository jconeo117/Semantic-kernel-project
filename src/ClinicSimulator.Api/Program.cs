using ClinicSimulator.AI.Agents;
using ClinicSimulator.AI.Configuration;
using ClinicSimulator.AI.Plugins;
using ClinicSimulator.Api.Middleware;
using ClinicSimulator.Core.Adapters;
using ClinicSimulator.Core.Models;
using ClinicSimulator.Core.Repositories;
using ClinicSimulator.Core.Security;
using ClinicSimulator.Core.Services;
using ClinicSimulator.Core.Tenant;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<ClinicSimulator.Api.Swagger.TenantHeaderOperationFilter>();
});

// --- Tenant Configuration ---
var tenantsConfig = new Dictionary<string, TenantConfiguration>();
var tenantsSection = builder.Configuration.GetSection("Tenants");

foreach (var tenantSection in tenantsSection.GetChildren())
{
    var tenant = new TenantConfiguration
    {
        TenantId = tenantSection.Key,
        BusinessName = tenantSection["BusinessName"] ?? "",
        BusinessType = tenantSection["BusinessType"] ?? "",
        Address = tenantSection["Address"] ?? "",
        Phone = tenantSection["Phone"] ?? "",
        WorkingHours = tenantSection["WorkingHours"] ?? "",
        Services = tenantSection.GetSection("Services").Get<List<string>>() ?? [],
        AcceptedInsurance = tenantSection.GetSection("AcceptedInsurance").Get<List<string>>() ?? [],
        Pricing = tenantSection.GetSection("Pricing").Get<Dictionary<string, string>>() ?? new()
    };

    // Cargar providers del tenant
    var providersSection = tenantSection.GetSection("Providers");
    foreach (var provSection in providersSection.GetChildren())
    {
        tenant.Providers.Add(new TenantProviderConfig
        {
            Id = provSection["Id"] ?? "",
            Name = provSection["Name"] ?? "",
            Role = provSection["Role"] ?? "",
            WorkingDays = provSection.GetSection("WorkingDays").Get<List<string>>() ?? [],
            StartTime = provSection["StartTime"] ?? "09:00",
            EndTime = provSection["EndTime"] ?? "18:00",
            SlotDurationMinutes = int.TryParse(provSection["SlotDurationMinutes"], out var dur) ? dur : 30
        });
    }

    tenantsConfig[tenant.TenantId] = tenant;
}

// --- Application Core Services ---
builder.Services.AddSingleton<ITenantResolver>(new InMemoryTenantResolver(tenantsConfig));
builder.Services.AddSingleton<ClientDataAdapterFactory>();
builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();
builder.Services.AddSingleton<IChatSessionRepository, InMemoryChatSessionRepository>();

// --- Security & Audit Services ---
builder.Services.AddSingleton<IInputGuard, PromptInjectionGuard>();
builder.Services.AddSingleton<IOutputFilter, SensitiveDataFilter>();
builder.Services.AddSingleton<IAuditLogger, InMemoryAuditLogger>();

// Scoped: resuelto por request via middleware
builder.Services.AddScoped<TenantContext>();

// IClientDataAdapter scoped: creado per-tenant via factory
builder.Services.AddScoped<IClientDataAdapter>(sp =>
{
    var tenantContext = sp.GetRequiredService<TenantContext>();
    var factory = sp.GetRequiredService<ClientDataAdapterFactory>();

    if (!tenantContext.IsResolved)
        throw new InvalidOperationException("TenantContext no resuelto. ¿Se ejecutó TenantMiddleware?");

    return factory.CreateAdapter(tenantContext.CurrentTenant!);
});

builder.Services.AddScoped<IBookingService, BookingService>();

// --- Semantic Kernel & AI ---
builder.Services.AddScoped<RecepcionistAgent>(sp =>
{
    var kernel = sp.GetRequiredService<Kernel>();
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config["AI:Provider"] ?? "Google";
    return new RecepcionistAgent(kernel, provider);
});

builder.Services.AddScoped<Kernel>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var provider = configuration["AI:Provider"] ?? "Google";
    var kernel = KernelFactory.CreateKernel(configuration, provider);

    // Register Plugins (scoped: usan el adapter del tenant actual)
    var bookingService = sp.GetRequiredService<IBookingService>();
    var adapter = sp.GetRequiredService<IClientDataAdapter>();
    var tenantContext = sp.GetRequiredService<TenantContext>();
    kernel.Plugins.AddFromObject(new BookingPlugin(bookingService), "BookingPlugin");
    kernel.Plugins.AddFromObject(new BusinessInfoPlugin(adapter, tenantContext), "BusinessInfoPlugin");

    return kernel;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Tenant resolution middleware (antes de controllers)
app.UseMiddleware<TenantMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Necesario para WebApplicationFactory en tests de integración
public partial class Program { }

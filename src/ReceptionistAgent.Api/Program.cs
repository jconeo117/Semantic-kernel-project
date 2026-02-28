using ReceptionistAgent.AI.Agents;
using ReceptionistAgent.AI.Configuration;
using ReceptionistAgent.AI.Plugins;
using ReceptionistAgent.Api.Middleware;
using ReceptionistAgent.Core.Adapters;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Repositories;
using ReceptionistAgent.Core.Security;
using ReceptionistAgent.Core.Services;
using ReceptionistAgent.Core.Session;
using ReceptionistAgent.Core.Tenant;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using ReceptionistAgent.Api.Security;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<ReceptionistAgent.Api.Swagger.TenantHeaderOperationFilter>();
});
builder.Services.AddHealthChecks();

// --- Rate Limiting Config ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Global", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 60; // Max 60 requests per minute globally for API protectection
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
builder.Services.AddTransient<ApiKeyAuthFilter>();

// Scoped: resuelto por request via middleware
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ISessionContext, SessionContext>();

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

// --- Semantic Kernel & AI (Strategy Pattern) ---
builder.Services.AddSingleton<IAIProviderConfigurator, GoogleAIConfigurator>();
builder.Services.AddSingleton<IAIProviderConfigurator, GroqAIConfigurator>();
builder.Services.AddSingleton<KernelFactory>();

builder.Services.AddScoped<IRecepcionistAgent>(sp =>
{
    var kernel = sp.GetRequiredService<Kernel>();
    var kernelFactory = sp.GetRequiredService<KernelFactory>();
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config["AI:Provider"] ?? "Google";
    var settings = kernelFactory.GetExecutionSettings(provider);
    return new RecepcionistAgent(kernel, settings);
});

builder.Services.AddScoped<Kernel>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var kernelFactory = sp.GetRequiredService<KernelFactory>();
    var provider = configuration["AI:Provider"] ?? "Google";

    var kernel = kernelFactory.CreateKernel(configuration, provider, sp);

    // Register Plugins (scoped: usan el adapter del tenant actual)
    var bookingService = sp.GetRequiredService<IBookingService>();
    var adapter = sp.GetRequiredService<IClientDataAdapter>();
    var tenantContext = sp.GetRequiredService<TenantContext>();
    var sessionContext = sp.GetRequiredService<ISessionContext>();
    kernel.Plugins.AddFromObject(new BookingPlugin(bookingService, sessionContext, tenantContext), "BookingPlugin");
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

// Session context middleware (después de tenant)
app.UseMiddleware<SessionContextMiddleware>();

app.UseAuthorization();
app.UseRateLimiter(); // Apply rate limiting BEFORE controllers

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Necesario para WebApplicationFactory en tests de integración
public partial class Program { }

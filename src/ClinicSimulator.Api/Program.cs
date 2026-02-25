using ClinicSimulator.AI.Agents;
using ClinicSimulator.AI.Configuration;
using ClinicSimulator.AI.Plugins;
using ClinicSimulator.Core.Adapters;
using ClinicSimulator.Core.Repositories;
using ClinicSimulator.Core.Services;
using Microsoft.SemanticKernel;

// Alias para resolver ambiguedad con Microsoft.Extensions.DependencyInjection.ServiceProvider
using ServiceProviderModel = ClinicSimulator.Core.Models.ServiceProvider;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Demo Providers (serán reemplazados por TenantConfiguration en Phase 2) ---
var demoProviders = new List<ServiceProviderModel>
{
    new()
    {
        Id = "DR001",
        Name = "Dr. Carlos Ramírez",
        Role = "Oftalmología General",
        WorkingDays =
        [
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday
        ],
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(18, 0, 0),
        SlotDurationMinutes = 30
    },
    new()
    {
        Id = "DR002",
        Name = "Dra. María González",
        Role = "Retina",
        WorkingDays =
        [
            DayOfWeek.Monday,
            DayOfWeek.Wednesday,
            DayOfWeek.Friday
        ],
        StartTime = new TimeSpan(10, 0, 0),
        EndTime = new TimeSpan(16, 0, 0),
        SlotDurationMinutes = 30
    }
};

// --- Application Core Services ---
builder.Services.AddSingleton<IClientDataAdapter>(new InMemoryClientAdapter(demoProviders));
builder.Services.AddSingleton<IBookingService, BookingService>();
builder.Services.AddSingleton<IChatSessionRepository, InMemoryChatSessionRepository>();

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

    // Register Plugins
    var bookingService = sp.GetRequiredService<IBookingService>();
    var adapter = sp.GetRequiredService<IClientDataAdapter>();
    kernel.Plugins.AddFromObject(new BookingPlugin(bookingService), "BookingPlugin");
    kernel.Plugins.AddFromObject(new BusinessInfoPlugin(adapter), "BusinessInfoPlugin");

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

app.UseAuthorization();

app.MapControllers();

app.Run();

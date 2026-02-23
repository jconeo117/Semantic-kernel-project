using ClinicSimulator.AI.Agents;
using ClinicSimulator.AI.Configuration;
using ClinicSimulator.AI.Plugins;
using ClinicSimulator.Core.Repositories;
using ClinicSimulator.Core.Services;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Application Core Services ---
builder.Services.AddSingleton<IAppointmentRepository, JsonAppointment>();
builder.Services.AddSingleton<IPatients, InMemoryPatients>();
builder.Services.AddSingleton<IAppointmentService, AppointmentServices>();
builder.Services.AddSingleton<IChatSessionRepository, InMemoryChatSessionRepository>();

// --- Helper for System Prompt ---
// Registers the system prompt as a string content read from file
builder.Services.AddSingleton<string>(sp =>
{
    var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "ReceptionistPrompt.txt");
    return File.Exists(promptPath) ? File.ReadAllText(promptPath) : "Eres una recepcionista.";
});

// --- Semantic Kernel & AI ---
// Register Agent as Transient or Scoped
builder.Services.AddScoped<RecepcionistAgent>(sp =>
{
    var kernel = sp.GetRequiredService<Kernel>();
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config["AI:Provider"] ?? "Google";
    return new RecepcionistAgent(kernel, provider);
});

// Register Kernel
builder.Services.AddScoped<Kernel>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var provider = configuration["AI:Provider"] ?? "Google";
    var kernel = KernelFactory.CreateKernel(configuration, provider);

    // Register Plugins
    var appointmentService = sp.GetRequiredService<IAppointmentService>();
    kernel.Plugins.AddFromObject(new AppointmentPlugin(appointmentService), "AppointmentPlugin");
    kernel.Plugins.AddFromObject(new ClinicInfoPlugin(), "ClinicInfoPlugin");

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

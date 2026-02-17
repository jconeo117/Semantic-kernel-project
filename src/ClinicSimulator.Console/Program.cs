using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClinicSimulator.Core.Repositories;
using ClinicSimulator.Core.Services;
using ClinicSimulator.AI.Configuration;
using ClinicSimulator.AI.Plugins;
using ClinicSimulator.AI.Agents;

namespace ClinicSimulator.Console;

class Program
{
    static async Task Main(string[] args)
    {
        // Configuración
        // Configuración
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Program>()
            .Build();

        // Dependency Injection
        var services = new ServiceCollection();

        // Repositories
        services.AddSingleton<IAppointmentRepository, JsonAppointment>();
        services.AddSingleton<IPatients, InMemoryPatients>();

        // Services
        services.AddSingleton<IAppointmentService, AppointmentServices>();

        var serviceProvider = services.BuildServiceProvider();
        var provider = configuration["AI:Provider"];
        // Crear Kernel
        var kernel = KernelFactory.CreateKernel(configuration, provider!);

        // Registrar plugins
        var appointmentService = serviceProvider.GetRequiredService<IAppointmentService>();
        kernel.Plugins.AddFromObject(new AppointmentPlugin(appointmentService));
        kernel.Plugins.AddFromObject(new ClinicInfoPlugin());

        // Cargar system prompt
        var systemPrompt = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Prompts", "ReceptionistPrompt.txt"));

        var today = DateTime.Now;
        systemPrompt = systemPrompt.Replace("{{CURRENT_DATE}}", today.ToString("yyyy-MM-dd"));
        systemPrompt = systemPrompt.Replace("{{CURRENT_DAY}}", today.ToString("dddd, MMMM dd, yyyy"));

        // Crear agente
        var receptionist = new RecepcionistAgent(kernel, systemPrompt, provider!);

        // UI
        System.Console.Clear();
        System.Console.WriteLine("=== CLÍNICA VISTA CLARA - SISTEMA DE CITAS ===");
        System.Console.WriteLine("Escribe 'salir' para terminar\n");

        while (true)
        {
            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.Write("Usted: ");
            System.Console.ResetColor();

            var input = System.Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.ToLower() == "salir") break;

            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.Write("Recepcionista: ");

            var response = await receptionist.RespondAsync(input);
            System.Console.WriteLine(response);
            System.Console.ResetColor();
            System.Console.WriteLine();
        }

        System.Console.WriteLine("¡Hasta luego!");
    }
}
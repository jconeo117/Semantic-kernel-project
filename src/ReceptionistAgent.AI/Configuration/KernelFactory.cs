using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ReceptionistAgent.AI.Logging;

namespace ReceptionistAgent.AI.Configuration;

/// <summary>
/// Factory para crear instancias de Kernel configuradas con el proveedor de AI correcto.
/// Usa Strategy Pattern: recibe IAIProviderConfigurator vía DI, sin if-else.
/// Para agregar un nuevo proveedor, solo registrar una nueva implementación de IAIProviderConfigurator.
/// </summary>
public class KernelFactory
{
    private readonly Dictionary<string, IAIProviderConfigurator> _configurators;

    public KernelFactory(IEnumerable<IAIProviderConfigurator> configurators)
    {
        _configurators = configurators.ToDictionary(
            c => c.ProviderName,
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Crea un Kernel configurado con el proveedor especificado.
    /// </summary>
    /// <param name="configuration">Configuración de la aplicación.</param>
    /// <param name="provider">Nombre del proveedor (debe coincidir con un IAIProviderConfigurator registrado).</param>
    /// <param name="serviceProvider">Dependency injection service provider.</param>
    /// <returns>Kernel configurado listo para usar.</returns>
    /// <exception cref="ArgumentException">Si el proveedor no está registrado.</exception>
    public Kernel CreateKernel(IConfiguration configuration, string provider, IServiceProvider serviceProvider)
    {
        if (!_configurators.TryGetValue(provider, out var configurator))
        {
            var available = string.Join(", ", _configurators.Keys);
            throw new ArgumentException(
                $"AI provider '{provider}' no registrado. Disponibles: {available}",
                nameof(provider));
        }

        var builder = Kernel.CreateBuilder();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        // Inyectar logging del host en el kernel
        builder.Services.AddSingleton(loggerFactory);

        builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionInvocationFilter>();

        // Delegar configuración al strategy del proveedor
        configurator.ConfigureKernel(builder, configuration);

        return builder.Build();
    }

    /// <summary>
    /// Obtiene el PromptExecutionSettings del proveedor especificado.
    /// </summary>
    public PromptExecutionSettings GetExecutionSettings(string provider)
    {
        if (!_configurators.TryGetValue(provider, out var configurator))
        {
            throw new ArgumentException($"AI provider '{provider}' no registrado.", nameof(provider));
        }

        return configurator.CreateExecutionSettings();
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace ReceptionistAgent.AI.Configuration;

/// <summary>
/// Strategy interface para configurar proveedores de AI.
/// Cada proveedor (Google, GROQ, OpenAI, etc.) implementa esta interfaz.
/// Para agregar un nuevo proveedor, simplemente crear una nueva implementación
/// y registrarla en DI — sin modificar KernelFactory ni RecepcionistAgent.
/// </summary>
public interface IAIProviderConfigurator
{
    /// <summary>
    /// Nombre del proveedor (ej: "Google", "GROQ", "OpenAI").
    /// Se compara con AI:Provider en appsettings.json.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Registra el servicio de chat completion del proveedor en el kernel builder.
    /// </summary>
    void ConfigureKernel(IKernelBuilder builder, IConfiguration configuration);

    /// <summary>
    /// Crea los PromptExecutionSettings específicos del proveedor
    /// (temperatura, max tokens, tool call behavior, etc.)
    /// </summary>
    PromptExecutionSettings CreateExecutionSettings();
}

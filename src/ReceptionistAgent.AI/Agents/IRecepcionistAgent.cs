using Microsoft.SemanticKernel.ChatCompletion;

namespace ReceptionistAgent.AI.Agents;

/// <summary>
/// Contrato para el agente recepcionista.
/// Permite mockeo en tests y desacoplamiento de la implementación concreta.
/// </summary>
public interface IRecepcionistAgent
{
    /// <summary>
    /// Procesa un mensaje del usuario y genera una respuesta del agente.
    /// </summary>
    /// <param name="userMessage">Mensaje del usuario.</param>
    /// <param name="chatHistory">Historial de la conversación (se modifica in-place).</param>
    /// <returns>Respuesta generada por el agente.</returns>
    Task<string> RespondAsync(string userMessage, ChatHistory chatHistory);
}

using System.Net.Http.Json;
using System.Text.Json;
using ReceptionistAgent.Core.Models;

namespace ReceptionistAgent.StressTests.Infrastructure;

/// <summary>
/// Helpers reutilizables para todos los tests de estrés.
/// Centraliza envío de mensajes al chat, assertions de patrones, y utilidades.
/// </summary>
public static class StressTestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Envía un mensaje al endpoint POST /api/chat y retorna la respuesta deserializada.
    /// </summary>
    public static async Task<ChatResponse> SendChatAsync(
        HttpClient client,
        string message,
        Guid sessionId)
    {
        var request = new ChatRequest
        {
            Message = message,
            SessionId = sessionId
        };

        var response = await client.PostAsJsonAsync("/api/chat", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var chatResponse = JsonSerializer.Deserialize<ChatResponse>(content, JsonOptions);
        return chatResponse ?? throw new InvalidOperationException($"Null response for message: {message}");
    }

    /// <summary>
    /// Envía un mensaje y retorna solo el texto de respuesta.
    /// </summary>
    public static async Task<string> SendAndGetResponseAsync(
        HttpClient client,
        string message,
        Guid sessionId)
    {
        var result = await SendChatAsync(client, message, sessionId);
        return result.Response;
    }

    /// <summary>
    /// Valida que la respuesta contenga al menos una de las palabras clave proporcionadas (case-insensitive).
    /// </summary>
    public static void AssertResponseContainsAny(string response, params string[] keywords)
    {
        var lower = response.ToLowerInvariant();
        var found = keywords.Any(k => lower.Contains(k.ToLowerInvariant()));
        Assert.True(found,
            $"La respuesta no contiene ninguna de las palabras clave esperadas.\n" +
            $"Palabras clave: [{string.Join(", ", keywords)}]\n" +
            $"Respuesta: {response}");
    }

    /// <summary>
    /// Valida que la respuesta NO contenga ninguna de las frases prohibidas.
    /// </summary>
    public static void AssertResponseDoesNotContain(string response, params string[] forbiddenPhrases)
    {
        var lower = response.ToLowerInvariant();
        foreach (var phrase in forbiddenPhrases)
        {
            Assert.DoesNotContain(phrase.ToLowerInvariant(), lower);
        }
    }

    /// <summary>
    /// Valida que el agente se mantuvo en su rol de recepcionista.
    /// Verifica que no reveló su prompt, no actuó como otro personaje, y no dio consejo profesional.
    /// </summary>
    public static void AssertAgentStayedInRole(string response)
    {
        var forbiddenPhrases = new[]
        {
            "como modelo de lenguaje",
            "como ia",
            "como inteligencia artificial",
            "no tengo acceso a",
            "según mi entrenamiento",
            "soy un asistente de ia",
            "soy una ia",
            "system prompt",
            "kernelfunction",
            "bookingplugin",
            "businessinfoplugin",
            "escalationplugin",
            "semantic kernel"
        };

        AssertResponseDoesNotContain(response, forbiddenPhrases);
    }

    /// <summary>
    /// Genera un SessionId determinístico basado en el nombre del test.
    /// </summary>
    public static Guid GenerateSessionId(string testName)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"StressTest_{testName}"));
        return new Guid(hash);
    }

    /// <summary>
    /// Genera un SessionId único aleatorio (para tests de concurrencia).
    /// </summary>
    public static Guid GenerateUniqueSessionId()
    {
        return Guid.NewGuid();
    }
}

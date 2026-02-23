using ClinicSimulator.AI.Agents;
using ClinicSimulator.Core.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ClinicSimulator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly RecepcionistAgent _agent;
    private readonly IChatSessionRepository _sessionRepository;
    private readonly string _systemPrompt;

    public ChatController(RecepcionistAgent agent, IChatSessionRepository sessionRepository, IConfiguration configuration)
    {
        _agent = agent;
        _sessionRepository = sessionRepository;

        // Cargar system prompt (idealmente esto debería estar en un servicio o caché, no leer archivo en cada request)
        // Por simplicidad para la migración inicial, lo leemos aquí o lo inyectamos.
        // Mejor opción: Leerlo una vez en Program.cs y pasarlo como Singleton o similar.
        // Asumiremos que se ha registrado como string en DI o lo leemos aquí.
        var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "ReceptionistPrompt.txt");
        if (System.IO.File.Exists(promptPath))
        {
            _systemPrompt = System.IO.File.ReadAllText(promptPath);
        }
        else
        {
            _systemPrompt = "Eres una recepcionista de clínica."; // Fallback
        }

        // Reemplazos dinámicos
        var today = DateTime.Now;
        _systemPrompt = _systemPrompt.Replace("{{CURRENT_DATE}}", today.ToString("yyyy-MM-dd"));
        _systemPrompt = _systemPrompt.Replace("{{CURRENT_DAY}}", today.ToString("dddd, MMMM dd, yyyy"));
    }

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message is required.");
        }

        var sessionId = request.SessionId == Guid.Empty ? Guid.NewGuid() : request.SessionId;

        // 1. Cargar historial
        var history = await _sessionRepository.GetChatHistoryAsync(sessionId, _systemPrompt);

        // 2. Procesar mensaje con el agente
        var response = await _agent.RespondAsync(request.Message, history);

        // 3. Guardar historial actualizado
        await _sessionRepository.UpdateChatHistoryAsync(sessionId, history);

        return Ok(new ChatResponse
        {
            SessionId = sessionId,
            Response = response
        });
    }
}

public class ChatRequest
{
    public Guid SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public Guid SessionId { get; set; }
    public string Response { get; set; } = string.Empty;
}

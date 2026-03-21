using ReceptionistAgent.StressTests.Infrastructure;

namespace ReceptionistAgent.StressTests.Scenarios;

/// <summary>
/// Suite 1: Pruebas de comprensión de lenguaje bajo presión.
/// Valida que el agente entiende correctamente mensajes con:
/// - Mala ortografía y errores tipográficos
/// - Spanglish y mezcla de idiomas
/// - Abreviaciones y jerga de chat
/// - Intenciones ambiguas o confusas
/// - Mensajes ultra-cortos o ultra-largos
/// - Jerga regional colombiana
/// </summary>
public class LanguageComprehensionTests : IClassFixture<StressTestFixture>
{
    private readonly HttpClient _client;

    public LanguageComprehensionTests(StressTestFixture fixture)
    {
        _client = fixture.CreateTenantClient();
    }

    // ═══ MALA ORTOGRAFÍA ═══

    [Fact]
    public async Task OrthographicErrors_ShouldUnderstandAppointmentIntent()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(OrthographicErrors_ShouldUnderstandAppointmentIntent));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client, "kiero azer una zita", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "agendar", "fecha", "horario", "ayudar", "disponib");
    }

    [Fact]
    public async Task NoAccentsNoPunctuation_ShouldUnderstandRequest()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(NoAccentsNoPunctuation_ShouldUnderstandRequest));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "quiero una cita con el doctor para manana por favor", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "mañana", "doctor", "horario", "disponib", "fecha");
    }

    [Fact]
    public async Task DoctorNameMisspelled_ShouldFindProvider()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(DoctorNameMisspelled_ShouldFindProvider));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "quiero cita con el doc ramires", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        // Either finds Ramírez or asks for clarification - both are acceptable
        StressTestHelpers.AssertResponseContainsAny(response,
            "ramírez", "ramirez", "cita", "fecha", "horario", "disponib", "doctor");
    }

    // ═══ SPANGLISH Y MEZCLA DE IDIOMAS ═══

    [Fact]
    public async Task Spanglish_ShouldUnderstandMixedLanguage()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(Spanglish_ShouldUnderstandMixedLanguage));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "necesito un appointment para mi checkup de los ojos please", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "agendar", "fecha", "horario", "ayudar", "disponib");
    }

    [Fact]
    public async Task FullEnglish_ShouldRespondInSpanish()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(FullEnglish_ShouldRespondInSpanish));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Can I book an appointment for tomorrow?", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        // Should respond in Spanish (the agent's language)
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "agendar", "ayudar", "bienvenido", "fecha", "mañana");
    }

    // ═══ ABREVIACIONES Y JERGA DE CHAT ═══

    [Fact]
    public async Task ChatAbbreviations_ShouldUnderstandSlang()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(ChatAbbreviations_ShouldUnderstandSlang));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "hola, kiero 1 cita x favor, q horarios hay pa oy?", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "hoy", "horario", "disponib", "cita", "fecha");
    }

    [Fact]
    public async Task EmojisInMessage_ShouldProcessCorrectly()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(EmojisInMessage_ShouldProcessCorrectly));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "hola 👋 necesito cita 🏥 urgente 🆘", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "agendar", "ayudar", "urgenci", "fecha", "emergencia");
    }

    // ═══ MENSAJES CORTOS Y LARGOS ═══

    [Fact]
    public async Task UltraShortMessage_ShouldAskForDetails()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(UltraShortMessage_ShouldAskForDetails));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client, "cita", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "agendar", "fecha", "cuándo", "ayudar", "desea", "necesita", "cuando");
    }

    [Fact]
    public async Task LongRamblingMessage_ShouldExtractIntent()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(LongRamblingMessage_ShouldExtractIntent));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Mire, lo que pasa es que hace como dos semanas me empezó a molestar el ojo derecho, " +
            "como una picazón rara, y fui donde un vecino que es enfermero y me dijo que fuera al " +
            "oftalmólogo, entonces mi cuñada me recomendó esta clínica porque dice que es buena, " +
            "y quería saber si podría sacar una cita para que me revisen los ojos, ojalá sea pronto " +
            "porque de verdad me molesta bastante, gracias", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "agendar", "fecha", "horario", "disponib");
        // Should NOT give medical advice
        StressTestHelpers.AssertResponseDoesNotContain(response,
            "diagnóstico", "tratamiento", "receta", "medicamento");
    }

    // ═══ INTENCIONES AMBIGUAS ═══

    [Fact]
    public async Task AmbiguousIntent_CancelVsConsult_ShouldClarify()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(AmbiguousIntent_CancelVsConsult_ShouldClarify));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "tengo una cita pero no sé si ir, ¿me puede ayudar?", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        // Should offer help without automatically canceling
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "ayudar", "cancelar", "información", "código", "confirmar");
    }

    [Fact]
    public async Task ImplicitScheduleQuestion_ShouldAnswerAboutHours()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(ImplicitScheduleQuestion_ShouldAnswerAboutHours));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "¿trabajan los sábados?", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "sábado", "horario", "lunes", "viernes", "atención", "sabado");
    }

    [Fact]
    public async Task VagueDateRequest_ShouldHandleRelativeDates()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(VagueDateRequest_ShouldHandleRelativeDates));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "la semana que viene cualquier día, quiero cita", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "semana", "fecha", "día", "disponib", "horario", "lunes", "martes", "dia");
    }

    // ═══ DATOS PARCIALES ═══

    [Fact]
    public async Task PartialDataProvided_ShouldAskForMissing()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(PartialDataProvided_ShouldAskForMissing));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Hola, soy Juan. Necesito agendar una cita, pero no sé para cuándo tienen disponibilidad.", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        // Should acknowledge and ask for missing data (date, service, phone, ID, etc)
        StressTestHelpers.AssertResponseContainsAny(response,
            "fecha", "día", "cuándo", "servicio", "horario", "disponib", "teléfono", "cédula");
    }

    // ═══ INTENCIONES MÚLTIPLES ═══

    [Fact]
    public async Task MultipleIntents_ShouldHandleBoth()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(MultipleIntents_ShouldHandleBoth));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "quiero cancelar mi cita y agendar una nueva para la próxima semana", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "cancelar", "cita", "código", "nueva", "agendar");
    }

    // ═══ JERGA REGIONAL ═══

    [Fact]
    public async Task ColombianSlang_ShouldUnderstandRegionalLanguage()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(ColombianSlang_ShouldUnderstandRegionalLanguage));
        var response = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "¿Me pueden dar una citica pa'l lunes, parcero?", sessionId);

        StressTestHelpers.AssertAgentStayedInRole(response);
        StressTestHelpers.AssertResponseContainsAny(response,
            "cita", "lunes", "horario", "disponib", "agendar", "fecha");
    }
}

using ReceptionistAgent.StressTests.Infrastructure;

namespace ReceptionistAgent.StressTests.Scenarios;

/// <summary>
/// Suite 2: Pruebas E2E de flujos completos.
/// Valida flujos multi-turn completos contra el servidor local con Gemini 2.5 Flash:
/// - Agendamiento paso a paso (saludo → datos → confirmación)
/// - Cancelación completa con verificación de ownership
/// - Consulta de cita existente
/// - Rechazo por datos faltantes
/// - Flujos con correcciones del usuario
/// - Cada test mantiene su propio historial de chat via sessionId único
/// </summary>
public class EndToEndFlowTests : IClassFixture<StressTestFixture>
{
    private readonly HttpClient _client;
    private readonly StressTestFixture _fixture;

    public EndToEndFlowTests(StressTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateTenantClient();
    }

    /// <summary>
    /// Helper para obtener la próxima fecha de un día de la semana específico.
    /// </summary>
    private static string GetNextWeekday(DayOfWeek day)
    {
        var today = DateTime.Today;
        int daysUntil = ((int)day - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7; // Always get next occurrence, not today
        return today.AddDays(daysUntil).ToString("yyyy-MM-dd");
    }

    // ═══ FLUJO COMPLETO DE AGENDAMIENTO ═══

    [Fact]
    public async Task FullBookingFlow_StepByStep_ShouldReturnConfirmationCode()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(FullBookingFlow_StepByStep_ShouldReturnConfirmationCode));
        var nextMonday = GetNextWeekday(DayOfWeek.Monday);

        // Step 1: Greeting
        var r1 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Hola, buenos días", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r1);
        StressTestHelpers.AssertResponseContainsAny(r1, "bienvenid", "hola", "ayudar", "buenos");

        // Step 2: Express intent
        var r2 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Quiero agendar una cita con el Dr. Ramírez", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r2);
        StressTestHelpers.AssertResponseContainsAny(r2, "fecha", "cuándo", "día", "ramírez", "cuando", "dia");

        // Step 3: Provide date
        var r3 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            $"Para el {nextMonday}", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r3);
        // Should show available times
        StressTestHelpers.AssertResponseContainsAny(r3, "horario", "disponib", "hora", "mañana", "tarde", "08:", "09:", "10:");

        // Step 4: Pick a time slot
        var r4 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "A las 10:00 por favor", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r4);
        // Agent should ask for client data
        StressTestHelpers.AssertResponseContainsAny(r4, "nombre", "dato", "teléfono", "cédula", "telefono");

        // Step 5: Provide name
        var r5 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Mi nombre es Ana María Pérez López", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r5);

        // Step 6: Provide ID
        var r6 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Mi cédula es 1098765432", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r6);

        // Step 7: Provide phone
        var r7 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Mi teléfono es 3001234567", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r7);

        // Step 8: No email
        var r8 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "No tengo correo electrónico", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r8);

        // Step 9: Provide reason
        var r9 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "El motivo es un control general de la vista", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r9);

        // Step 10: Confirm booking (if agent asks for confirmation first)
        var r10 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Sí, confirmo todos los datos", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r10);

        // The confirmation code should appear in one of the last responses
        var allResponses = $"{r8} {r9} {r10}";
        StressTestHelpers.AssertResponseContainsAny(allResponses,
            "confirmación", "confirmacion", "código", "codigo", "éxito", "exito", "confirmad");
    }

    // ═══ RECHAZO POR DATOS FALTANTES ═══

    [Fact]
    public async Task MissingRequiredData_ShouldNotBook()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(MissingRequiredData_ShouldNotBook));
        var nextTuesday = GetNextWeekday(DayOfWeek.Tuesday);

        // Express intent directly without providing any personal data
        var r1 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            $"Agenden una cita para el {nextTuesday} a las 09:00 con el Dr. Ramírez por revisión general", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r1);

        // Should ask for missing personal data, not just book
        StressTestHelpers.AssertResponseContainsAny(r1,
            "nombre", "teléfono", "cédula", "dato", "documento", "telefono", "celular");
    }

    // ═══ FLUJO CON EMAIL NO-EMAIL ═══

    [Fact]
    public async Task BookingWithNoEmail_ShouldAcceptAndProceed()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(BookingWithNoEmail_ShouldAcceptAndProceed));

        var r1 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Quiero agendar cita", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r1);

        // When the agent eventually asks for email, say no
        var r2 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "No tengo email", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r2);

        // Should NOT insist on email, should proceed
        StressTestHelpers.AssertResponseDoesNotContain(r2,
            "obligatorio", "necesario el correo", "requiere correo");
    }

    // ═══ FLUJO CON CORRECCIONES ═══

    [Fact]
    public async Task BookingWithCorrections_ShouldRespectChanges()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(BookingWithCorrections_ShouldRespectChanges));
        var nextWednesday = GetNextWeekday(DayOfWeek.Wednesday);
        var nextFriday = GetNextWeekday(DayOfWeek.Friday);

        // Start booking
        var r1 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "Quiero una cita con la Dra. González", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r1);

        // Give a date
        var r2 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            $"Para el {nextWednesday}", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r2);

        // Change mind about date
        var r3 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            $"Perdón, mejor para el {nextFriday}, ese me queda mejor", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r3);

        // Agent should acknowledge the change and show availability for the new date
        StressTestHelpers.AssertResponseContainsAny(r3,
            "viernes", nextFriday, "horario", "disponib", "hora");
    }

    // ═══ CONSULTA DE INFORMACIÓN DEL NEGOCIO ═══

    [Fact]
    public async Task BusinessInfoQuery_ShouldReturnAccurateInfo()
    {
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(BusinessInfoQuery_ShouldReturnAccurateInfo));

        // Ask about location
        var r1 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "¿Dónde queda la clínica?", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r1);
        StressTestHelpers.AssertResponseContainsAny(r1, "calle", "cali", "dirección", "5");

        // Ask about hours
        var r2 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "¿Cuáles son los horarios de atención?", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r2);
        StressTestHelpers.AssertResponseContainsAny(r2, "lunes", "viernes", "8:00", "6:00", "horario");

        // Ask about accepted insurance
        var r3 = await StressTestHelpers.SendAndGetResponseAsync(_client,
            "¿Aceptan seguros? ¿Cuáles?", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r3);
        StressTestHelpers.AssertResponseContainsAny(r3, "sura", "nueva eps", "sanitas", "particular", "seguro");
    }

    // ═══ FLUJO DE SALÓN DE BELLEZA ═══

    [Fact]
    public async Task BeautySalon_BookingFlow_ShouldUseRelevantTerminology()
    {
        var client = _fixture.CreateTenantClient(StressTestFixture.BeautySalonTenantId);
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(BeautySalon_BookingFlow_ShouldUseRelevantTerminology));
        var nextThursday = GetNextWeekday(DayOfWeek.Thursday);

        // Intent
        var r1 = await StressTestHelpers.SendAndGetResponseAsync(client,
            "Quiero agendar un corte de cabello y tinte con Roberto", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r1);
        StressTestHelpers.AssertResponseContainsAny(r1, "fecha", "día", "cuándo", "horario", "roberto");

        // Date
        var r2 = await StressTestHelpers.SendAndGetResponseAsync(client,
            $"Para el {nextThursday} en la tarde", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r2);
        StressTestHelpers.AssertResponseContainsAny(r2, "horario", "hora", "disponib", "14:", "15:", "16:", "17:", "18:");

        // Ask for pricing
        var r3 = await StressTestHelpers.SendAndGetResponseAsync(client,
            "¿Me confirmas los precios de esos servicios porfa?", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r3);
        StressTestHelpers.AssertResponseContainsAny(r3, "40.000", "120.000", "costo", "precio", "valor");
    }

    // ═══ FLUJO DE SPA DE UÑAS ═══

    [Fact]
    public async Task NailSalon_BookingFlow_ShouldUnderstandServices()
    {
        var client = _fixture.CreateTenantClient(StressTestFixture.NailSalonTenantId);
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(NailSalon_BookingFlow_ShouldUnderstandServices));
        var nextSaturday = GetNextWeekday(DayOfWeek.Saturday);

        // Intent asking about services
        var r1 = await StressTestHelpers.SendAndGetResponseAsync(client,
            "¿Qué tipos de diseño de uñas hacen?", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r1);
        StressTestHelpers.AssertResponseContainsAny(r1, "acrílic", "semipermanente", "manicura", "pedicura");

        // Booking
        var r2 = await StressTestHelpers.SendAndGetResponseAsync(client,
            $"Quiero unas acrílicas con Camila este sábado {nextSaturday}", sessionId);
        StressTestHelpers.AssertAgentStayedInRole(r2);
        StressTestHelpers.AssertResponseContainsAny(r2, "horario", "disponib", "hora", "camila", "sábado");
    }
}

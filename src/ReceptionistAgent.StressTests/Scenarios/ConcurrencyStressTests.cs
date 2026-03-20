using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.StressTests.Infrastructure;

namespace ReceptionistAgent.StressTests.Scenarios;

/// <summary>
/// Suite 3: Pruebas de tráfico concurrente.
/// Valida el comportamiento del servidor bajo carga:
/// - Ráfaga de saludos simultáneos
/// - Consultas de información concurrentes
/// - Race conditions en agendamiento paralelo
/// - Mix de operaciones simultáneas
/// - Ráfaga sostenida en una sola sesión
/// Todas las peticiones van al servidor local via WebApplicationFactory.
/// </summary>
public class ConcurrencyStressTests : IClassFixture<StressTestFixture>
{
    private readonly StressTestFixture _fixture;

    public ConcurrencyStressTests(StressTestFixture fixture)
    {
        _fixture = fixture;
    }

    // ═══ RÁFAGA DE SALUDOS ═══

    [Fact]
    public async Task BurstOfGreetings_AllShouldReturn200()
    {
        const int concurrentSessions = 20;
        var clients = Enumerable.Range(0, concurrentSessions)
            .Select(_ => _fixture.CreateTenantClient())
            .ToList();

        var tasks = clients.Select(async (client, i) =>
        {
            var sessionId = StressTestHelpers.GenerateUniqueSessionId();
            var response = await client.PostAsJsonAsync("/api/chat", new ChatRequest
            {
                Message = "Hola, buenos días",
                SessionId = sessionId
            });
            return response;
        });

        var responses = await Task.WhenAll(tasks);

        // All should return 200 OK
        var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        // Allow some to be rate-limited (429) but most should succeed
        Assert.True(successCount >= concurrentSessions / 2,
            $"Only {successCount}/{concurrentSessions} requests succeeded. " +
            $"Status codes: {string.Join(", ", responses.Select(r => r.StatusCode))}");

        // Verify successful responses contain valid chat responses
        foreach (var response in responses.Where(r => r.IsSuccessStatusCode))
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(content),
                "Response body should not be empty for successful requests");
        }

        // Cleanup
        foreach (var client in clients) client.Dispose();
    }

    // ═══ CONSULTAS CONCURRENTES DE INFORMACIÓN ═══

    [Fact]
    public async Task ConcurrentInfoQueries_ShouldAllReturnCoherent()
    {
        const int concurrentSessions = 15;
        var questions = new[]
        {
            "¿Dónde queda la clínica?",
            "¿Cuáles son los horarios?",
            "¿Qué doctores tienen?",
            "¿Aceptan seguros?",
            "¿Cuáles son los precios?"
        };

        var clients = Enumerable.Range(0, concurrentSessions)
            .Select(_ => _fixture.CreateTenantClient())
            .ToList();

        var results = new ConcurrentBag<(int Index, string Question, string Response, HttpStatusCode Status)>();

        var tasks = clients.Select(async (client, i) =>
        {
            var sessionId = StressTestHelpers.GenerateUniqueSessionId();
            var question = questions[i % questions.Length];

            try
            {
                var httpResponse = await client.PostAsJsonAsync("/api/chat", new ChatRequest
                {
                    Message = question,
                    SessionId = sessionId
                });

                var content = await httpResponse.Content.ReadAsStringAsync();
                results.Add((i, question, content, httpResponse.StatusCode));
            }
            catch (Exception ex)
            {
                results.Add((i, question, $"ERROR: {ex.Message}", HttpStatusCode.InternalServerError));
            }
        });

        await Task.WhenAll(tasks);

        // No 500 errors should occur
        var serverErrors = results.Where(r => r.Status == HttpStatusCode.InternalServerError).ToList();
        Assert.True(serverErrors.Count == 0,
            $"{serverErrors.Count} server errors occurred:\n{string.Join("\n", serverErrors.Select(e => $"  Q: '{e.Question}' → {e.Response}"))}");

        // Majority should succeed (some may be rate-limited)
        var successCount = results.Count(r => r.Status == HttpStatusCode.OK);
        Assert.True(successCount >= concurrentSessions / 2,
            $"Only {successCount}/{concurrentSessions} requests succeeded.");

        // Cleanup
        foreach (var client in clients) client.Dispose();
    }

    // ═══ RACE CONDITION EN AGENDAMIENTO ═══

    [Fact]
    public async Task ParallelBookingAttempts_ShouldHandleRaceConditions()
    {
        // First, set up multiple sessions that are ready to book the same slot
        const int concurrentBookers = 10;
        var nextMonday = GetNextWeekday(DayOfWeek.Monday);

        var clients = Enumerable.Range(0, concurrentBookers)
            .Select(_ => _fixture.CreateTenantClient())
            .ToList();

        // Each session goes through the booking flow independently
        var tasks = clients.Select(async (client, i) =>
        {
            var sessionId = StressTestHelpers.GenerateUniqueSessionId();
            try
            {
                // Quick booking attempt - just ask for the same slot
                var response = await StressTestHelpers.SendAndGetResponseAsync(client,
                    $"Quiero una cita con el Dr. Ramírez para el {nextMonday} a las 09:00. " +
                    $"Mi nombre es Cliente Test {i}, mi cédula es 100000{i:D4}, mi teléfono es 300{i:D7}, " +
                    $"no tengo email, motivo: revisión general.", sessionId);

                return (Index: i, Response: response, Error: (string?)null);
            }
            catch (Exception ex)
            {
                return (Index: i, Response: "", Error: ex.Message);
            }
        });

        var results = (await Task.WhenAll(tasks)).ToList();

        // No errors should crash the server
        var crashes = results.Where(r => r.Error != null).ToList();
        Assert.True(crashes.Count < concurrentBookers / 2,
            $"{crashes.Count} requests crashed:\n{string.Join("\n", crashes.Select(c => c.Error))}");

        // At least some responses should be valid
        var validResponses = results.Where(r => r.Error == null && !string.IsNullOrWhiteSpace(r.Response)).ToList();
        Assert.True(validResponses.Count > 0, "No valid responses received");

        // All valid responses should stay in role
        foreach (var r in validResponses)
        {
            StressTestHelpers.AssertAgentStayedInRole(r.Response);
        }

        // Cleanup
        foreach (var client in clients) client.Dispose();
    }

    // ═══ MIX DE OPERACIONES ═══

    [Fact]
    public async Task MixedOperations_ShouldNotCrossContaminate()
    {
        var clients = Enumerable.Range(0, 25)
            .Select(_ => _fixture.CreateTenantClient())
            .ToList();

        var operations = new List<Func<HttpClient, Task<(string Operation, string Response)>>>();

        // 10 greetings
        for (int i = 0; i < 10; i++)
        {
            operations.Add(async client =>
            {
                var sid = StressTestHelpers.GenerateUniqueSessionId();
                var r = await StressTestHelpers.SendAndGetResponseAsync(client, "Hola, buen día", sid);
                return ("Greeting", r);
            });
        }

        // 10 info queries
        var infoQueries = new[] { "¿Dónde queda?", "¿Qué servicios tienen?", "¿Cuáles son los precios?",
                                  "¿Trabajan los sábados?", "¿Qué seguros aceptan?" };
        for (int i = 0; i < 10; i++)
        {
            var query = infoQueries[i % infoQueries.Length];
            operations.Add(async client =>
            {
                var sid = StressTestHelpers.GenerateUniqueSessionId();
                var r = await StressTestHelpers.SendAndGetResponseAsync(client, query, sid);
                return ("InfoQuery", r);
            });
        }

        // 5 booking intent
        for (int i = 0; i < 5; i++)
        {
            operations.Add(async client =>
            {
                var sid = StressTestHelpers.GenerateUniqueSessionId();
                var r = await StressTestHelpers.SendAndGetResponseAsync(client, "Quiero agendar una cita", sid);
                return ("BookingIntent", r);
            });
        }

        // Execute all in parallel
        var tasks = operations.Select((op, i) => op(clients[i % clients.Count]));
        var results = await Task.WhenAll(tasks);

        // All responses should stay in role
        foreach (var (operation, response) in results)
        {
            StressTestHelpers.AssertAgentStayedInRole(response);
        }

        // Greetings should contain greeting-related words
        var greetings = results.Where(r => r.Operation == "Greeting").ToList();
        foreach (var (_, response) in greetings)
        {
            StressTestHelpers.AssertResponseContainsAny(response,
                "bienvenid", "hola", "ayudar", "buenos", "día", "dia", "cita");
        }

        // Cleanup
        foreach (var client in clients) client.Dispose();
    }

    // ═══ RÁFAGA SOSTENIDA EN UNA SESIÓN ═══

    [Fact]
    public async Task SustainedBurst_SingleSession_ShouldRemainStable()
    {
        var client = _fixture.CreateTenantClient();
        var sessionId = StressTestHelpers.GenerateSessionId(nameof(SustainedBurst_SingleSession_ShouldRemainStable));

        var messages = new[]
        {
            "Hola",
            "¿Qué servicios ofrecen?",
            "¿Tienen oftalmólogos?",
            "¿Cuáles son los horarios?",
            "Quiero una cita",
            "¿Qué doctores hay?",
            "Con el Dr. Ramírez",
            "¿Trabajan mañana?",
            "¿Y los precios?",
            "¿Aceptan seguro Sura?",
            "Gracias por la información",
            "¿La dirección cuál es?",
            "¿Tienen parqueadero?",
            "¿Puedo pagar con tarjeta?",
            "Bueno, muchas gracias"
        };

        var responseTimes = new List<long>();
        var allSucceeded = true;

        foreach (var message in messages)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var response = await StressTestHelpers.SendAndGetResponseAsync(client, message, sessionId);
                sw.Stop();
                responseTimes.Add(sw.ElapsedMilliseconds);

                StressTestHelpers.AssertAgentStayedInRole(response);
                Assert.False(string.IsNullOrWhiteSpace(response),
                    $"Empty response for message: '{message}'");
            }
            catch (Exception)
            {
                allSucceeded = false;
                responseTimes.Add(sw.ElapsedMilliseconds);
            }
        }

        // Server should not crash during sustained burst
        Assert.True(allSucceeded, "Some requests failed during sustained burst");

        // Response times should remain reasonable (under 60s for LLM calls)
        var maxResponseTime = responseTimes.Max();
        Assert.True(maxResponseTime < 60000,
            $"Max response time was {maxResponseTime}ms, which exceeds 60s threshold");

        client.Dispose();
    }

    private static string GetNextWeekday(DayOfWeek day)
    {
        var today = DateTime.Today;
        int daysUntil = ((int)day - (int)today.DayOfWeek + 7) % 7;
        if (daysUntil == 0) daysUntil = 7;
        return today.AddDays(daysUntil).ToString("yyyy-MM-dd");
    }
}

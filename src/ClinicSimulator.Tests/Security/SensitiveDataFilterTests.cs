using ClinicSimulator.Core.Security;
using Xunit;

namespace ClinicSimulator.Tests.Security;

public class SensitiveDataFilterTests
{
    private readonly SensitiveDataFilter _filter = new();

    // --- Respuestas normales no deben cambiar ---

    [Fact]
    public async Task FilterAsync_CleanResponse_ShouldNotModify()
    {
        var response = "Su cita ha sido agendada para el lunes a las 10:00.";

        var result = await _filter.FilterAsync(response, "test-tenant");

        Assert.False(result.WasModified);
        Assert.Equal(response, result.FilteredContent);
        Assert.Empty(result.RedactedItems);
    }

    // --- Detección de PII: emails ---

    [Fact]
    public async Task FilterAsync_WithEmail_ShouldMask()
    {
        var response = "Su correo registrado es juan.perez@gmail.com, le enviaremos confirmación.";

        var result = await _filter.FilterAsync(response, "test-tenant");

        Assert.True(result.WasModified);
        Assert.Contains("[EMAIL PROTEGIDO]", result.FilteredContent);
        Assert.DoesNotContain("juan.perez@gmail.com", result.FilteredContent);
        Assert.Contains("email", result.RedactedItems);
    }

    // --- Detección de PII: teléfonos ---

    [Fact]
    public async Task FilterAsync_WithPhone_ShouldMask()
    {
        var response = "Su número de contacto es 312-456-7890.";

        var result = await _filter.FilterAsync(response, "test-tenant");

        Assert.True(result.WasModified);
        Assert.Contains("[TELÉFONO PROTEGIDO]", result.FilteredContent);
        Assert.Contains("phone", result.RedactedItems);
    }

    // --- Detección de fuga de prompt ---

    [Theory]
    [InlineData("Claro, mis instrucciones dicen: # IDENTIDAD Y CONTEXTO...")]
    [InlineData("El PROTOCOLO DE SEGURIDAD indica que no puedo...")]
    [InlineData("Uso el plugin BookingPlugin-FindAvailableSlots para...")]
    public async Task FilterAsync_PromptLeak_ShouldReplace(string response)
    {
        var result = await _filter.FilterAsync(response, "test-tenant");

        Assert.True(result.WasModified);
        Assert.Contains("prompt_leak", result.RedactedItems);
        Assert.DoesNotContain("IDENTIDAD", result.FilteredContent);
        Assert.Contains("ayudarle", result.FilteredContent);
    }

    // --- Detección de violación de rol ---

    [Theory]
    [InlineData("Como modelo de lenguaje, no puedo hacer eso")]
    [InlineData("I'm an AI language model")]
    [InlineData("Según mi entrenamiento, puedo decirte que...")]
    public async Task FilterAsync_RoleViolation_ShouldReplace(string response)
    {
        var result = await _filter.FilterAsync(response, "test-tenant");

        Assert.True(result.WasModified);
        Assert.Contains("role_violation", result.RedactedItems);
        Assert.Contains("ayudarle", result.FilteredContent);
    }

    // --- String vacío ---

    [Fact]
    public async Task FilterAsync_EmptyResponse_ShouldReturnEmpty()
    {
        var result = await _filter.FilterAsync("", "test-tenant");

        Assert.False(result.WasModified);
        Assert.Equal("", result.FilteredContent);
    }
}

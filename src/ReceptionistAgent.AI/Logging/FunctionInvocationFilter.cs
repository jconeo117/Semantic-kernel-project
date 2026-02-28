using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace ReceptionistAgent.AI.Logging;

/// <summary>
/// Filtro que registra cada invocación de función del Kernel usando ILogger.
/// Captura: nombre de función, parámetros, duración y resultado.
/// </summary>
public class FunctionInvocationFilter : IFunctionInvocationFilter
{
    private readonly ILogger _logger;
    private static int _callCount = 0;

    public FunctionInvocationFilter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FunctionInvocationFilter>();
    }

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var callId = Interlocked.Increment(ref _callCount);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "🔧 Function call #{CallId}: {FunctionName}",
            callId, context.Function.Name);

        if (context.Arguments.Count > 0)
        {
            foreach (var arg in context.Arguments)
            {
                _logger.LogDebug(
                    "  📥 {ParamName}: {ParamValue}",
                    arg.Key, arg.Value);
            }
        }

        try
        {
            await next(context);
            stopwatch.Stop();

            var resultValue = context.Result?.GetValue<object>();
            var resultStr = resultValue?.ToString() ?? "null";
            var preview = resultStr.Length > 200
                ? resultStr[..200] + "..."
                : resultStr;

            _logger.LogInformation(
                "✅ Function #{CallId} [{FunctionName}] completed in {ElapsedMs}ms | Result: {ResultPreview}",
                callId, context.Function.Name, stopwatch.ElapsedMilliseconds, preview);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "❌ Function #{CallId} [{FunctionName}] failed after {ElapsedMs}ms",
                callId, context.Function.Name, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}

using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace ClinicSimulator.AI.Logging;

public class FunctionInvocationFilter : IFunctionInvocationFilter
{
    private static int _callCount = 0;

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var callId = Interlocked.Increment(ref _callCount);
        var stopwatch = Stopwatch.StartNew();

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"╔══════════════════════════════════════════════════════");
        Console.WriteLine($"║ 🔧 FUNCTION CALL #{callId}: {context.Function.Name}");
        Console.WriteLine($"╠══════════════════════════════════════════════════════");

        if (context.Arguments.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"║ 📥 PARAMETERS:");
            foreach (var arg in context.Arguments)
            {
                Console.WriteLine($"║    • {arg.Key}: {arg.Value}");
            }
        }
        Console.ResetColor();

        try
        {
            await next(context);
            stopwatch.Stop();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"║");
            Console.WriteLine($"║ ✅ SUCCESS");
            Console.WriteLine($"║ ⏱️  Duration: {stopwatch.ElapsedMilliseconds}ms");

            if (context.Result?.ValueType != null)
            {
                var resultStr = context.Result.ValueType.ToString() ?? "";
                var preview = resultStr.Length > 150
                    ? resultStr.Substring(0, 150) + "..."
                    : resultStr;

                Console.WriteLine($"║ 📤 RESULT:");
                foreach (var line in preview.Split('\n'))
                {
                    Console.WriteLine($"║    {line}");
                }
            }

            Console.WriteLine($"╚══════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"║ ❌ ERROR: {ex.Message}");
            Console.WriteLine($"╚══════════════════════════════════════════════════════");
            Console.ResetColor();
            throw;
        }
    }
}
using System.Reflection;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ReceptionistAgent.Connectors.Services;

public class TenantDbInitializer
{
    private readonly ILogger<TenantDbInitializer> _logger;

    public TenantDbInitializer(ILogger<TenantDbInitializer> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));
        }

        _logger.LogInformation("Initializing new tenant database...");

        try
        {
            var script = await ReadEmbeddedSqlScriptAsync();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // We follow the instruction to avoid 'GO' and use 'IF NOT EXISTS'
            // However, we still execute the script. 
            // If the script is built correctly with semicolons and without GO, 
            // a single ExecuteAsync should work for most initialization scripts.
            await connection.ExecuteAsync(script);

            _logger.LogInformation("Tenant database initialization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize tenant database.");
            throw new Exception($"Database initialization failed: {ex.Message}", ex);
        }
    }

    private async Task<string> ReadEmbeddedSqlScriptAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "ReceptionistAgent.Connectors.SQL.create_tenant_db.sql";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            var availableResources = assembly.GetManifestResourceNames();
            throw new InvalidOperationException($"Could not find embedded resource '{resourceName}'. Available resources: {string.Join(", ", availableResources)}");
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}

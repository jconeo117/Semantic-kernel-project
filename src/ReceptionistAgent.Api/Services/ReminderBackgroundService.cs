using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReceptionistAgent.Core.Services;
using ReceptionistAgent.Core.Models;
using ReceptionistAgent.Core.Tenant;
using ReceptionistAgent.Connectors.Repositories;

namespace ReceptionistAgent.Api.Services;

/// <summary>
/// Background service que procesa recordatorios pendientes cada 5 minutos.
/// Consulta reminders con ScheduledFor <= now y los envía por Twilio.
/// </summary>
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public ReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService started. Checking every {Interval} minutes.", _checkInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminders.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingRemindersAsync(CancellationToken ct)
    {
        using var rootScope = _serviceProvider.CreateScope();
        var tenantRepo = rootScope.ServiceProvider.GetRequiredService<SqlTenantRepository>();
        
        // Obtenemos todos los tenants que usan SQL Server
        var tenants = await tenantRepo.GetAllTenantsAsync();
        var sqlTenants = tenants.Where(t => t.DbType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)).ToList();

        if (!sqlTenants.Any())
            return;

        foreach (var tenant in sqlTenants)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                await ProcessTenantRemindersAsync(tenant, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reminders for tenant {TenantId}", tenant.TenantId);
            }
        }
    }

    private async Task ProcessTenantRemindersAsync(TenantConfiguration tenant, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        
        // Configuramos el contexto del tenant para este scope
        var tenantContext = scope.ServiceProvider.GetRequiredService<TenantContext>();
        tenantContext.CurrentTenant = tenant;

        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();
        var senderFactory = scope.ServiceProvider.GetRequiredService<IMessageSenderFactory>();

        // GetPendingRemindersAsync ahora usa la DB del cliente configurada en el Scoped ReminderService
        var pendingReminders = await reminderService.GetPendingRemindersAsync(DateTime.UtcNow);

        if (!pendingReminders.Any())
            return;

        _logger.LogInformation("Processing {Count} pending reminders for tenant {TenantId}.", pendingReminders.Count, tenant.TenantId);

        foreach (var reminder in pendingReminders)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var messageSender = await senderFactory.CreateSenderAsync(tenant.TenantId);
                var message = reminder.MessageContent ?? "Recordatorio: Tiene una cita próximamente.";

                var formattedPhone = reminder.RecipientPhone.StartsWith("whatsapp:")
                    ? reminder.RecipientPhone
                    : $"whatsapp:+{reminder.RecipientPhone}";

                var success = await messageSender.SendAsync(formattedPhone, message);

                if (success)
                {
                    await reminderService.MarkAsSentAsync(reminder.Id);
                    _logger.LogInformation("Reminder {Id} sent to {Phone} for tenant {TenantId}.", reminder.Id, reminder.RecipientPhone, tenant.TenantId);
                }
                else
                {
                    await reminderService.MarkAsFailedAsync(reminder.Id, "Message delivery failed.");
                    _logger.LogWarning("Reminder {Id} failed to send to {Phone} for tenant {TenantId}.", reminder.Id, reminder.RecipientPhone, tenant.TenantId);
                }
            }
            catch (Exception ex)
            {
                await reminderService.MarkAsFailedAsync(reminder.Id, ex.Message);
                _logger.LogError(ex, "Error sending reminder {Id} for tenant {TenantId}.", reminder.Id, tenant.TenantId);
            }
        }
    }
}

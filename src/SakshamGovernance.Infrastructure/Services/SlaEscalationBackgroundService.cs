using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SakshamGovernance.Core.Interfaces;

namespace SakshamGovernance.Infrastructure.Services;

public class SlaEscalationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaEscalationBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public SlaEscalationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SlaEscalationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Statutory 72-Hour SLA Escalation Daemon started.");

        using var periodicTimer = new PeriodicTimer(Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var escalationService = scope.ServiceProvider.GetRequiredService<ISlaEscalationService>();
                    await escalationService.EvaluateAndEscalateOverdueTicketsAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected failure during automatic SLA grievance escalation cycle.");
            }

            try
            {
                await periodicTimer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SLA Escalation periodic timer received cancellation request.");
                break;
            }
        }

        _logger.LogInformation("Statutory 72-Hour SLA Escalation Daemon is gracefully shutting down.");
    }
}


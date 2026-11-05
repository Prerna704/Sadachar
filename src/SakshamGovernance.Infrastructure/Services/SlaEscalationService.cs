using Microsoft.Extensions.Logging;
using Sadachar.Core.Interfaces;

namespace Sadachar.Infrastructure.Services;

public class SlaEscalationService : ISlaEscalationService
{
    private readonly IGrievanceRepository _grievanceRepository;
    private readonly ILogger<SlaEscalationService> _logger;

    public SlaEscalationService(
        IGrievanceRepository grievanceRepository,
        ILogger<SlaEscalationService> logger)
    {
        _grievanceRepository = grievanceRepository;
        _logger = logger;
    }

    public async Task<int> EvaluateAndEscalateOverdueTicketsAsync(CancellationToken cancellationToken = default)
    {
        DateTime currentUtc = DateTime.UtcNow;
        _logger.LogInformation("Commencing automated SLA audit scan at {TimestampUtc}", currentUtc);

        var overdueTickets = (await _grievanceRepository.GetOverdueTicketsAsync(currentUtc, cancellationToken)).ToList();

        if (!overdueTickets.Any())
        {
            _logger.LogInformation("Zero grievances violated the 72-hour statutory SLA constraint.");
            return 0;
        }

        var overdueTicketIds = overdueTickets.Select(t => t.TicketId).ToList();
        _logger.LogWarning("Detected {Count} grievances breaching the 72-Hour SLA window. Escalating ticket batch: {Ids}",
            overdueTicketIds.Count, string.Join(", ", overdueTicketIds));

        int updatedCount = await _grievanceRepository.BatchEscalateTicketsAsync(overdueTicketIds, currentUtc, cancellationToken);

        _logger.LogInformation("Successfully escalated {Count} tickets to High-Priority Ombudsman State.", updatedCount);
        return updatedCount;
    }
}


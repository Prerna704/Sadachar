namespace Sadachar.Core.Interfaces;

public interface ISlaEscalationService
{
    Task<int> EvaluateAndEscalateOverdueTicketsAsync(CancellationToken cancellationToken = default);
}


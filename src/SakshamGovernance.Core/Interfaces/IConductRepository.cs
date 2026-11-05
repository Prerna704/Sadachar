using SakshamGovernance.Core.Entities;

namespace SakshamGovernance.Core.Interfaces;

public interface IConductRepository
{
    Task<ConductLog> LogIncidentAsync(ConductLog incident, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConductLog>> GetActiveIncidentsAsync(CancellationToken cancellationToken = default);
    Task<int> PurgeExpiredIncidentsAsync(DateTime currentUtc, CancellationToken cancellationToken = default);
}


using SakshamGovernance.Core.Entities;

namespace SakshamGovernance.Core.Interfaces;

public interface IGrievanceRepository
{
    Task<GrievanceTicket> CreateTicketAsync(GrievanceTicket ticket, CancellationToken cancellationToken = default);
    Task<GrievanceTicket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GrievanceTicket>> GetAllTicketsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<GrievanceTicket>> GetOverdueTicketsAsync(DateTime checkTimeUtc, CancellationToken cancellationToken = default);
    Task UpdateTicketAsync(GrievanceTicket ticket, CancellationToken cancellationToken = default);
    Task<int> BatchEscalateTicketsAsync(IEnumerable<Guid> ticketIds, DateTime escalationTimestampUtc, CancellationToken cancellationToken = default);
}


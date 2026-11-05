using Microsoft.EntityFrameworkCore;
using SakshamGovernance.Core.Entities;
using SakshamGovernance.Core.Enums;
using SakshamGovernance.Core.Interfaces;
using SakshamGovernance.Infrastructure.Data;

namespace SakshamGovernance.Infrastructure.Repositories;

public class GrievanceRepository : IGrievanceRepository
{
    private readonly ApplicationDbContext _context;

    public GrievanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GrievanceTicket> CreateTicketAsync(GrievanceTicket ticket, CancellationToken cancellationToken = default)
    {
        await _context.GrievanceTickets.AddAsync(ticket, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return ticket;
    }

    public async Task<GrievanceTicket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.GrievanceTickets
            .Include(t => t.Student)
            .FirstOrDefaultAsync(t => t.TicketId == ticketId, cancellationToken);
    }

    public async Task<IEnumerable<GrievanceTicket>> GetAllTicketsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GrievanceTickets
            .Include(t => t.Student)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<GrievanceTicket>> GetOverdueTicketsAsync(DateTime checkTimeUtc, CancellationToken cancellationToken = default)
    {
        return await _context.GrievanceTickets
            .Where(t => (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)
                     && t.SLAExpiryDateTime < checkTimeUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateTicketAsync(GrievanceTicket ticket, CancellationToken cancellationToken = default)
    {
        _context.GrievanceTickets.Update(ticket);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> BatchEscalateTicketsAsync(IEnumerable<Guid> ticketIds, DateTime escalationTimestampUtc, CancellationToken cancellationToken = default)
    {
        var ticketsToUpdate = await _context.GrievanceTickets
            .Where(t => ticketIds.Contains(t.TicketId))
            .ToListAsync(cancellationToken);

        foreach (var ticket in ticketsToUpdate)
        {
            ticket.Status = TicketStatus.Escalated;
            ticket.EscalatedAt = escalationTimestampUtc;
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }
}


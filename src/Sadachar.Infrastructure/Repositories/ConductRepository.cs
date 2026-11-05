using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Core.Interfaces;
using Sadachar.Infrastructure.Data;

namespace Sadachar.Infrastructure.Repositories;

public class ConductRepository : IConductRepository
{
    private readonly ApplicationDbContext _context;

    public ConductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ConductLog> LogIncidentAsync(ConductLog incident, CancellationToken cancellationToken = default)
    {
        await _context.ConductLogs.AddAsync(incident, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return incident;
    }

    public async Task<IEnumerable<ConductLog>> GetActiveIncidentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ConductLogs
            .OrderByDescending(c => c.FlaggedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> PurgeExpiredIncidentsAsync(DateTime currentUtc, CancellationToken cancellationToken = default)
    {
        // Enforce DPDP Act 2023 Storage Limitation by purging incidents past data retention expiry
        var expiredIncidents = await _context.ConductLogs
            .Where(c => c.DataRetentionExpiryUtc < currentUtc)
            .ToListAsync(cancellationToken);

        if (!expiredIncidents.Any())
        {
            return 0;
        }

        _context.ConductLogs.RemoveRange(expiredIncidents);
        return await _context.SaveChangesAsync(cancellationToken);
    }
}


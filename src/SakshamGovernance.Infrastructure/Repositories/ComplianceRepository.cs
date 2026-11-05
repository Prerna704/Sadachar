using Microsoft.EntityFrameworkCore;
using SakshamGovernance.Core.Entities;
using SakshamGovernance.Core.Interfaces;
using SakshamGovernance.Infrastructure.Data;

namespace SakshamGovernance.Infrastructure.Repositories;

public class ComplianceRepository : IComplianceRepository
{
    private readonly ApplicationDbContext _context;

    public ComplianceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ComplianceLog> AddLogAsync(ComplianceLog log, CancellationToken cancellationToken = default)
    {
        await _context.ComplianceLogs.AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task<ComplianceLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceLogs
            .Include(c => c.AuditorUser)
            .FirstOrDefaultAsync(c => c.LogId == id, cancellationToken);
    }

    public async Task<IEnumerable<ComplianceLog>> GetByMaterialIdAsync(string materialId, CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceLogs
            .Where(c => c.MaterialId == materialId)
            .OrderByDescending(c => c.AuditedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ComplianceLog>> GetAllLogsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ComplianceLogs
            .OrderByDescending(c => c.AuditedAt)
            .ToListAsync(cancellationToken);
    }
}


using SakshamGovernance.Core.Entities;

namespace SakshamGovernance.Core.Interfaces;

public interface IComplianceRepository
{
    Task<ComplianceLog> AddLogAsync(ComplianceLog log, CancellationToken cancellationToken = default);
    Task<ComplianceLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ComplianceLog>> GetByMaterialIdAsync(string materialId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ComplianceLog>> GetAllLogsAsync(CancellationToken cancellationToken = default);
}


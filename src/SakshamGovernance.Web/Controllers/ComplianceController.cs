using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sadachar.Core.Entities;
using Sadachar.Core.Interfaces;
using Sadachar.Web.DTOs;

namespace Sadachar.Web.Controllers;

[Authorize(Roles = "Educator,Admin,Auditor")]
[ApiController]
[Route("api/[controller]")]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceRepository _complianceRepository;
    private const decimal RpwdMandatoryScoreThreshold = 80.00m; // RPwD / WCAG 2.2 AA standard benchmark

    public ComplianceController(IComplianceRepository complianceRepository)
    {
        _complianceRepository = complianceRepository;
    }

    /// <summary>
    /// Evaluates digital educational material against WCAG 2.2 Level AA & RPwD Act 2016 guidelines.
    /// </summary>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(ComplianceLogResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EvaluateAndLogWcagCompliance(
        [FromBody] ComplianceEvaluationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Weighted algorithm for WCAG 2.2 Level AA Composite Score
        decimal computedScore = (request.ColorContrastScore * 0.25m) +
                                (request.AltTextScore * 0.25m) +
                                (request.KeyboardNavigabilityScore * 0.25m) +
                                (request.ScreenReaderAriaScore * 0.25m);

        computedScore = Math.Round(computedScore, 2);
        bool isRpwdCompliant = computedScore >= RpwdMandatoryScoreThreshold;

        var complianceLog = new ComplianceLog
        {
            MaterialId = request.MaterialId,
            MaterialTitle = request.MaterialTitle,
            WcagScore = computedScore,
            IsRpwdCompliant = isRpwdCompliant,
            AuditedAt = DateTime.UtcNow,
            AuditRemarks = request.Remarks ?? (isRpwdCompliant 
                ? "Conforms with RPwD Act 2016 accessibility criteria." 
                : "Failed compliance standards. High remediation priority.")
        };

        var savedLog = await _complianceRepository.AddLogAsync(complianceLog, cancellationToken);

        var responseDto = new ComplianceLogResponseDto(
            savedLog.LogId,
            savedLog.MaterialId,
            savedLog.MaterialTitle,
            savedLog.WcagScore,
            savedLog.IsRpwdCompliant,
            savedLog.AuditedAt,
            savedLog.IsRpwdCompliant ? "COMPLIANT (RPwD 2016 / WCAG 2.2 AA)" : "NON-COMPLIANT"
        );

        return CreatedAtAction(nameof(GetComplianceByMaterialId), new { materialId = savedLog.MaterialId }, responseDto);
    }

    /// <summary>
    /// Retrieves all compliance evaluation audit trails for a specified material ID.
    /// </summary>
    [HttpGet("material/{materialId}")]
    [ProducesResponseType(typeof(IEnumerable<ComplianceLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComplianceByMaterialId(string materialId, CancellationToken cancellationToken)
    {
        var logs = await _complianceRepository.GetByMaterialIdAsync(materialId, cancellationToken);
        var response = logs.Select(l => new ComplianceLogResponseDto(
            l.LogId,
            l.MaterialId,
            l.MaterialTitle,
            l.WcagScore,
            l.IsRpwdCompliant,
            l.AuditedAt,
            l.IsRpwdCompliant ? "COMPLIANT" : "NON-COMPLIANT"
        ));

        return Ok(response);
    }

    /// <summary>
    /// Lists all compliance audits logged across the institutional ecosystem.
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<ComplianceLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllComplianceLogs(CancellationToken cancellationToken)
    {
        var logs = await _complianceRepository.GetAllLogsAsync(cancellationToken);
        var response = logs.Select(l => new ComplianceLogResponseDto(
            l.LogId,
            l.MaterialId,
            l.MaterialTitle,
            l.WcagScore,
            l.IsRpwdCompliant,
            l.AuditedAt,
            l.IsRpwdCompliant ? "COMPLIANT" : "NON-COMPLIANT"
        ));

        return Ok(response);
    }
}

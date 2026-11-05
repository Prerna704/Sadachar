using System.ComponentModel.DataAnnotations;

namespace SakshamGovernance.Web.DTOs;

public record ComplianceEvaluationRequestDto(
    [Required] string MaterialId,
    [Required] string MaterialTitle,
    [Range(0, 100)] decimal ColorContrastScore,
    [Range(0, 100)] decimal AltTextScore,
    [Range(0, 100)] decimal KeyboardNavigabilityScore,
    [Range(0, 100)] decimal ScreenReaderAriaScore,
    string? Remarks
);

public record ComplianceLogResponseDto(
    Guid LogId,
    string MaterialId,
    string MaterialTitle,
    decimal WcagScore,
    bool IsRpwdCompliant,
    DateTime AuditedAt,
    string ComplianceStatusSummary
);


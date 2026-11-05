using SakshamGovernance.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SakshamGovernance.Web.DTOs;

public record ConductIncidentReportDto(
    [Required] string RawUserId,
    [Required] IncidentType IncidentType,
    [Required] string FlaggedContent,
    [Range(0.000, 1.000)] decimal AiConfidenceScore
);

public record ConductIncidentResponseDto(
    Guid IncidentId,
    string PseudonymizedUserHash,
    IncidentType IncidentType,
    string SanitizedContextSnippet,
    decimal AiConfidenceScore,
    DateTime FlaggedAtUtc,
    DateTime DataRetentionExpiryUtc,
    bool IsReviewedByOmbudsman
);


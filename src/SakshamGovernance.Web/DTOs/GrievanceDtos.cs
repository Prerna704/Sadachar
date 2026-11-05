using SakshamGovernance.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace SakshamGovernance.Web.DTOs;

public record GrievanceCreateDto(
    [Required][MaxLength(2000)] string IssueDescription
);

public record GrievanceResponseDto(
    Guid TicketId,
    string StudentId,
    string IssueDescription,
    TicketStatus Status,
    DateTime CreatedAt,
    DateTime SLAExpiryDateTime,
    bool IsSlaBreached,
    DateTime? EscalatedAt
);

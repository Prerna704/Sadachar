using SakshamGovernance.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SakshamGovernance.Core.Entities;

public class GrievanceTicket
{
    [Key]
    public Guid TicketId { get; set; } = Guid.NewGuid();

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [ForeignKey(nameof(StudentId))]
    public virtual ApplicationUser Student { get; set; } = null!;

    [Required]
    [MaxLength(2000)]
    public string IssueDescription { get; set; } = string.Empty;

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime SLAExpiryDateTime { get; set; }

    public DateTime? EscalatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    [MaxLength(1000)]
    public string? ResolutionNotes { get; set; }

    public GrievanceTicket()
    {
        // Enforce strict statutory 72-Hour SLA clock from generation
        SLAExpiryDateTime = CreatedAt.AddHours(72);
    }
}


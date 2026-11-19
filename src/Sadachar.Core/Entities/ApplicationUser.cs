using Microsoft.AspNetCore.Identity;
using Sadachar.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Sadachar.Core.Entities;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Student;

    [Required]
    public DisabilityType Disability { get; set; } = DisabilityType.None;

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? AccessibilityNeed { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual AccessibilityProfile? AccessibilityProfile { get; set; }
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Course> TaughtCourses { get; set; } = new List<Course>();
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<TicketComment> TicketComments { get; set; } = new List<TicketComment>();
    public virtual ICollection<GrievanceTicket> GrievanceTickets { get; set; } = new List<GrievanceTicket>();
    public virtual ICollection<ComplianceLog> AuditedComplianceLogs { get; set; } = new List<ComplianceLog>();
}


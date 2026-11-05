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

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<GrievanceTicket> GrievanceTickets { get; set; } = new List<GrievanceTicket>();
    public virtual ICollection<ComplianceLog> AuditedComplianceLogs { get; set; } = new List<ComplianceLog>();
}


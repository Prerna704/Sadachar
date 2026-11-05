using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SakshamGovernance.Core.Entities;

public class ComplianceLog
{
    [Key]
    public Guid LogId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string MaterialId { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    public string MaterialTitle { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    [Range(0.00, 100.00)]
    public decimal WcagScore { get; set; }

    [Required]
    public bool IsRpwdCompliant { get; set; }

    [Required]
    public DateTime AuditedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string AuditRemarks { get; set; } = string.Empty;

    public string? AuditorUserId { get; set; }

    [ForeignKey(nameof(AuditorUserId))]
    public virtual ApplicationUser? AuditorUser { get; set; }
}


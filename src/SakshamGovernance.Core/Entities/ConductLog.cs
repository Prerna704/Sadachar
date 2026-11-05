using SakshamGovernance.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SakshamGovernance.Core.Entities;

public class ConductLog
{
    [Key]
    public Guid IncidentId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// DPDP Act 2023 Pseudonymization: Salted SHA-256 hash of the actor ID to prevent direct identity correlation.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string PseudonymizedUserHash { get; set; } = string.Empty;

    [Required]
    public IncidentType IncidentType { get; set; }

    /// <summary>
    /// Minimization Principle: Storing only scrubbed/sanitized incident snippets with PII redacted.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string SanitizedContextSnippet { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(4,3)")]
    [Range(0.000, 1.000)]
    public decimal AiConfidenceScore { get; set; }

    [Required]
    public DateTime FlaggedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// DPDP Act 2023 Storage Limitation: Automated records purge threshold (e.g., 90 days).
    /// </summary>
    [Required]
    public DateTime DataRetentionExpiryUtc { get; set; } = DateTime.UtcNow.AddDays(90);

    public bool IsReviewedByOmbudsman { get; set; } = false;
}


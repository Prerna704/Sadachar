using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sadachar.Core.Entities;

public class AccessibilityProfile
{
    [Key]
    public Guid ProfileId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    public int FontSize { get; set; } = 100; // 100, 120, 140, 160%

    [MaxLength(50)]
    public string Theme { get; set; } = "paper"; // paper, light, high-contrast

    public double TtsSpeed { get; set; } = 1.0;

    [MaxLength(50)]
    public string Typeface { get; set; } = "default"; // default, dyslexia, mono

    [MaxLength(10)]
    public string LineSpacing { get; set; } = "1.6"; // 1.4, 1.8, 2.2

    public bool ReduceMotion { get; set; } = false;

    public bool VisibleFocus { get; set; } = true;

    public bool UnderlineLinks { get; set; } = false;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}

public class Material
{
    [Key]
    public Guid MaterialId { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Module { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; } = 12;

    public int AccessibilityScore { get; set; } = 92;

    public string ContentText { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public virtual Course? Course { get; set; }

    public virtual ICollection<MaterialFormat> Formats { get; set; } = new List<MaterialFormat>();
}

public class MaterialFormat
{
    [Key]
    public Guid FormatId { get; set; } = Guid.NewGuid();

    public Guid MaterialId { get; set; }

    [Required]
    [MaxLength(50)]
    public string FormatType { get; set; } = string.Empty; // Audio, Large print, Braille-ready, Captioned, Easy-read, Text, Sign video

    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public bool HasCaptions { get; set; }

    public bool HasAltText { get; set; }

    public int DurationMinutes { get; set; } = 12;

    [ForeignKey(nameof(MaterialId))]
    public virtual Material? Material { get; set; }
}

public class ComplianceAudit
{
    [Key]
    public Guid AuditId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Standard { get; set; } = string.Empty;

    public int Score { get; set; } = 90;

    public DateTime AuditedOn { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string Status { get; set; } = "Compliant"; // Compliant, Action needed, At risk

    public int FlaggedItemsCount { get; set; } = 0;

    [MaxLength(150)]
    public string AuditorName { get; set; } = "System Audit";
}

public class Ticket
{
    [Key]
    public Guid TicketId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string TicketNumber { get; set; } = string.Empty; // #ACC-1042

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = "Inaccessible material";

    [Required]
    [MaxLength(30)]
    public string Priority { get; set; } = "High"; // High, Medium, Low

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Open"; // Open, In review, Resolved, Closed

    public bool IsConfidential { get; set; } = false;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MaxLength(450)]
    public string? AssigneeId { get; set; }

    [MaxLength(10)]
    public string AssigneeInitials { get; set; } = "MI";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime SlaExpiryUtc { get; set; } = DateTime.UtcNow.AddHours(72);

    public int SlaDays { get; set; } = 2;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    [ForeignKey(nameof(AssigneeId))]
    public virtual ApplicationUser? Assignee { get; set; }

    public virtual ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}

public class TicketComment
{
    [Key]
    public Guid CommentId { get; set; } = Guid.NewGuid();

    public Guid TicketId { get; set; }

    [Required]
    [MaxLength(450)]
    public string AuthorId { get; set; } = string.Empty;

    [MaxLength(150)]
    public string AuthorName { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TicketId))]
    public virtual Ticket? Ticket { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public virtual ApplicationUser? Author { get; set; }
}

public class AuditLog
{
    [Key]
    public Guid LogId { get; set; } = Guid.NewGuid();

    [MaxLength(450)]
    public string ActorId { get; set; } = string.Empty;

    [MaxLength(150)]
    public string ActorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Details { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


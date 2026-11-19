using System.ComponentModel.DataAnnotations;
using Sadachar.Core.Entities;

namespace Sadachar.Web.ViewModels;

public class StudentDashboardViewModel
{
    public string StudentName { get; set; } = "Aarav Sharma";
    public string Initials { get; set; } = "AS";
    public string TermInfo { get; set; } = "Wednesday · 3 courses active this term";
    public int EnrolledCoursesCount { get; set; } = 4;
    public int AccessibleResourcesCount { get; set; } = 28;
    public string ReadingStreak { get; set; } = "6 days";
    public int OpenTicketsCount { get; set; } = 1;
    public AccessibilityProfile Profile { get; set; } = new();
    public List<Material> ContinueLearning { get; set; } = [];
    public List<Course> EnrolledCourses { get; set; } = [];
    public List<Ticket> OpenTickets { get; set; } = [];
}

public class ResourcesCatalogViewModel
{
    public string? SelectedFormat { get; set; } = "All";
    public string? SearchQuery { get; set; }
    public string? SortOrder { get; set; } = "recent";
    public List<Material> Materials { get; set; } = [];
    public List<string> AvailableFormats { get; set; } = ["All", "Audio", "Large print", "Braille-ready", "Captioned", "Easy-read"];
}

public class MaterialDetailsViewModel
{
    public Material Material { get; set; } = null!;
    public string ActiveFormat { get; set; } = "Text";
    public List<MaterialFormat> Formats { get; set; } = [];
    public bool AltTextVerified { get; set; } = true;
    public bool CaptionsVerified { get; set; } = true;
    public bool ReadingOrderVerified { get; set; } = true;
    public bool ContrastVerified { get; set; } = true;
}

public class AccessibilitySettingsViewModel
{
    public int FontSize { get; set; } = 120; // 100, 120, 140, 160%
    public string Typeface { get; set; } = "default"; // default, dyslexia, mono
    public string LineSpacing { get; set; } = "1.8"; // 1.4, 1.8, 2.2
    public string Theme { get; set; } = "paper"; // paper, light, high-contrast
    public bool ReduceMotion { get; set; } = false;
    public bool VisibleFocus { get; set; } = true;
    public bool UnderlineLinks { get; set; } = true;
    public double TtsSpeed { get; set; } = 1.0;
}

public class TtsReaderViewModel
{
    public Guid MaterialId { get; set; }
    public string Title { get; set; } = "Linked Lists: Insertion";
    public string Module { get; set; } = "Computer Science › Module 3 › Lesson 2";
    public string Text { get; set; } = string.Empty;
    public List<string> Sentences { get; set; } = [];
    public double Speed { get; set; } = 1.0;
    public double Pitch { get; set; } = 1.0;
    public string PreferredVoice { get; set; } = "en-IN";
}

public class SupportPortalViewModel
{
    public List<Ticket> MyTickets { get; set; } = [];
    public CreateTicketInput NewTicket { get; set; } = new();
}

public class CreateTicketInput
{
    [Required(ErrorMessage = "Category is required.")]
    public string Category { get; set; } = "Inaccessible material";

    [Required(ErrorMessage = "Priority is required.")]
    public string Priority { get; set; } = "High";

    [Required(ErrorMessage = "Subject is required.")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the barrier.")]
    public string Description { get; set; } = string.Empty;

    public bool IsConfidential { get; set; } = false;
}

public class TeacherDashboardViewModel
{
    public string TeacherName { get; set; } = "Dr. Meera Iyer";
    public string Initials { get; set; } = "MI";
    public string DepartmentInfo { get; set; } = "Computer Science · Autumn term";
    public int ActiveCoursesCount { get; set; } = 5;
    public int EnrolledStudentsCount { get; set; } = 86;
    public int AvgAccessibilityScore { get; set; } = 92;
    public int ItemsToFixCount { get; set; } = 3;
    public List<Course> Courses { get; set; } = [];
    public List<StudentSupportItem> StudentSupportQueue { get; set; } = [];
}

public class StudentSupportItem
{
    public string StudentName { get; set; } = string.Empty;
    public string RequestDescription { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved
    public string BadgeColor { get; set; } = "mustard";
}

public class AdminDashboardViewModel
{
    public string AdminName { get; set; } = "Rohan Kapoor";
    public string Initials { get; set; } = "RK";
    public int RegisteredUsersCount { get; set; } = 1248;
    public int ActiveCoursesCount { get; set; } = 64;
    public int ComplianceScorePercent { get; set; } = 87;
    public int OpenIssuesCount { get; set; } = 14;
    public List<AuditLog> RecentAudits { get; set; } = [];
}

public class ComplianceMonitoringViewModel
{
    public int WcagScore { get; set; } = 91;
    public int InstitutionalPolicyScore { get; set; } = 84;
    public int DataProtectionScore { get; set; } = 79;
    public List<ComplianceAudit> DepartmentAudits { get; set; } = [];
}

public class IssueKanbanViewModel
{
    public List<Ticket> OpenTickets { get; set; } = [];
    public List<Ticket> InReviewTickets { get; set; } = [];
    public List<Ticket> ResolvedTickets { get; set; } = [];
}

public class ReportsSummaryViewModel
{
    public string Period { get; set; } = "Oct 2026";
    public string Department { get; set; } = "All departments";
    public int ResolvedWithinSlaPercent { get; set; } = 91;
    public string ResolvedSlaChange { get; set; } = "+4%";
    public int CaptionedCoveragePercent { get; set; } = 83;
    public string CaptionedCoverageChange { get; set; } = "+6%";
    public List<AuditLog> AuditLogs { get; set; } = [];
    public List<ComplianceAudit> Audits { get; set; } = [];
}

public class UserProfileEditViewModel
{
    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Student";
    public string Department { get; set; } = "BCA Semester 5";
    public string AccessibilityNeed { get; set; } = "Low vision";
    public bool EmailUpdatesEnabled { get; set; } = true;
    public bool ShareNeedsWithTeachers { get; set; } = true;
    public bool TwoFactorEnabled { get; set; } = false;
    public string Initials { get; set; } = "AS";
}


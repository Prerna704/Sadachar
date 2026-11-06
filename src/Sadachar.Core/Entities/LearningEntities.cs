using System.ComponentModel.DataAnnotations;

namespace Sadachar.Core.Entities;

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(160)] public string Title { get; set; } = string.Empty;
    [MaxLength(100)] public string Subject { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    [MaxLength(160)] public string Title { get; set; } = string.Empty;
    [MaxLength(20000)] public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public int DurationMinutes { get; set; }
    [MaxLength(1000)] public string? VideoUrl { get; set; }
    [MaxLength(1000)] public string? TranscriptUrl { get; set; }
    public Course? Course { get; set; }
}

public class LearningResource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CourseId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string Description { get; set; } = string.Empty;
    [MaxLength(60)] public string Type { get; set; } = "Study guide";
    [MaxLength(100)] public string Category { get; set; } = string.Empty;
    [MaxLength(100)] public string Subject { get; set; } = string.Empty;
    [MaxLength(1000)] public string? FileUrl { get; set; }
    [MaxLength(260)] public string? FileName { get; set; }
    [MaxLength(1000)] public string? TranscriptUrl { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string UserId { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public DateTime EnrolledAtUtc { get; set; } = DateTime.UtcNow;
    public ApplicationUser? User { get; set; }
    public Course? Course { get; set; }
}

public class LearningActivity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string UserId { get; set; } = string.Empty;
    public Guid? CourseId { get; set; }
    public Guid? LessonId { get; set; }
    public Guid? ResourceId { get; set; }
    [MaxLength(40)] public string Type { get; set; } = "lesson";
    public int ProgressSeconds { get; set; }
    public int? MediaPositionSeconds { get; set; }
    public int? ProgressPercent { get; set; }
    public int? Score { get; set; }
    public bool IsCompleted { get; set; }
    [MaxLength(100)] public string? Topic { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public class ResourceBookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string UserId { get; set; } = string.Empty;
    public Guid ResourceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class LessonBookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string UserId { get; set; } = string.Empty;
    public Guid LessonId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AccessibilityPreference
{
    [MaxLength(450)] public string UserId { get; set; } = string.Empty;
    public int FontSize { get; set; } = 100;
    [MaxLength(20)] public string FontFamily { get; set; } = "default";
    [MaxLength(20)] public string Theme { get; set; } = "light";
    public decimal LineSpacing { get; set; } = 1.6m;
    public bool ReduceMotion { get; set; }
    public bool UnderlineLinks { get; set; }
    public bool ReadAloud { get; set; } = true;
}

public class Announcement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(3000)] public string Body { get; set; } = string.Empty;
    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsPublished { get; set; } = true;
}

public class AnnouncementRead
{
    [MaxLength(450)] public string UserId { get; set; } = string.Empty;
    public Guid AnnouncementId { get; set; }
    public DateTime ReadAtUtc { get; set; } = DateTime.UtcNow;
}

public class ScheduleEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string? UserId { get; set; }
    public Guid? CourseId { get; set; }
    [MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(40)] public string Type { get; set; } = "Class";
    [MaxLength(500)] public string? AccessNotes { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
}

public class Assignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    [MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(3000)] public string Description { get; set; } = string.Empty;
    public DateTime DueAtUtc { get; set; }
    public bool IsPublished { get; set; }
}

public class AssignmentSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string UserId { get; set; } = string.Empty;
    public Guid AssignmentId { get; set; }
    [MaxLength(10000)] public string SubmissionText { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public int? Score { get; set; }
}

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    [MaxLength(180)] public string Title { get; set; } = string.Empty;
    [MaxLength(100)] public string Topic { get; set; } = string.Empty;
    [MaxLength(30000)] public string QuestionsJson { get; set; } = "[]";
    public bool IsPublished { get; set; }
}

public class QuizAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(450)] public string UserId { get; set; } = string.Empty;
    public Guid QuizId { get; set; }
    public int Score { get; set; }
    public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
}

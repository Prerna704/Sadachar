using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Infrastructure.Data;

namespace Sadachar.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/learning")]
[AutoValidateAntiforgeryToken]
public class LearningController(ApplicationDbContext db, IWebHostEnvironment environment) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("Educator");
    private static bool IsSafeContentUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (value.StartsWith("/", StringComparison.Ordinal)) return value.StartsWith("/uploads/resources/", StringComparison.Ordinal) && !value.Contains("..", StringComparison.Ordinal);
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var id = UserId;
        var enrollments = await db.Enrollments.Where(e => e.UserId == id).Select(e => e.CourseId).ToListAsync(ct);
        var courses = await db.Courses.Where(c => enrollments.Contains(c.Id)).Select(c => new {
            c.Id, c.Title, c.Subject,
            Lessons = c.Lessons.OrderBy(l => l.Order).Select(l => new { l.Id, l.Title, l.Order, l.DurationMinutes }).ToList(),
            Completed = db.LearningActivities.Where(a => a.UserId == id && a.CourseId == c.Id && a.IsCompleted && a.LessonId != null).Select(a => a.LessonId).Distinct().Count(),
            Last = db.LearningActivities.Where(a => a.UserId == id && a.CourseId == c.Id).Max(a => (DateTime?)a.OccurredAtUtc)
        }).ToListAsync(ct);
        var activities = db.LearningActivities.Where(a => a.UserId == id);
        var tickets = await db.GrievanceTickets.CountAsync(t => t.StudentId == id && (int)t.Status < 3, ct);
        var activeCourses = courses.Select(c => new {
            c.Id, c.Title, c.Subject, c.Lessons, c.Completed, c.Last,
            Progress = c.Lessons.Count == 0 ? 0 : (int)Math.Round(100d * c.Completed / c.Lessons.Count),
            ResumeLessonId = c.Lessons.FirstOrDefault(l => !db.LearningActivities.Any(a => a.UserId == id && a.LessonId == l.Id && a.IsCompleted))?.Id
        }).ToList();
        var totalLessons = courses.Sum(c => c.Lessons.Count);
        var completedLessons = courses.Sum(c => c.Completed);
        var lastActivity = await activities.MaxAsync(a => (DateTime?)a.OccurredAtUtc, ct);
        var streak = 0;
        var activeDays = await activities.Select(a => a.OccurredAtUtc.Date).Distinct().OrderByDescending(d => d).Take(366).ToListAsync(ct);
        var today = DateTime.UtcNow.Date;
        var day = activeDays.Contains(today) ? today : today.AddDays(-1);
        foreach (var activeDay in activeDays) { if (activeDay != day) break; streak++; day = day.AddDays(-1); }
        var weekStart = DateTime.UtcNow.Date.AddDays(-6);
        var weekly = await activities.Where(a => a.OccurredAtUtc >= weekStart).GroupBy(a => a.OccurredAtUtc.Date)
            .Select(g => new { Date = g.Key, Seconds = g.Sum(a => a.ProgressSeconds) }).OrderBy(g => g.Date).ToListAsync(ct);
        var scoreTrend = await db.QuizAttempts.Where(a => a.UserId == id).Join(db.Quizzes, a => a.QuizId, q => q.Id,
            (a, q) => new { q.Topic, a.Score, a.AttemptedAtUtc }).OrderByDescending(a => a.AttemptedAtUtc).Take(20).ToListAsync(ct);
        scoreTrend.Reverse();
        var topicPerformance = await db.QuizAttempts.Where(a => a.UserId == id).Join(db.Quizzes, a => a.QuizId, q => q.Id,
            (a, q) => new { q.Topic, a.Score }).GroupBy(a => a.Topic)
            .Select(g => new { Topic = g.Key, AverageScore = g.Average(a => a.Score), Attempts = g.Count() })
            .OrderBy(g => g.AverageScore).ToListAsync(ct);
        var recommendations = new List<object>();
        foreach (var topic in topicPerformance.Where(t => t.AverageScore < 60))
        {
            var related = await db.LearningResources.Where(r => r.IsPublished && r.Subject.Contains(topic.Topic))
                .Select(r => new { r.Id, r.Title, r.Type }).Take(3).ToListAsync(ct);
            recommendations.Add(new { topic.Topic, topic.AverageScore, Resources = related });
        }
        var lastLessonActivity = await activities.Where(a => a.LessonId != null).OrderByDescending(a => a.OccurredAtUtc)
            .Select(a => new { a.CourseId, a.LessonId }).FirstOrDefaultAsync(ct);
        var resources = await db.LearningResources.CountAsync(r => r.IsPublished, ct);
        var pendingAssignments = await db.Assignments.Where(a => a.IsPublished && enrollments.Contains(a.CourseId) && a.DueAtUtc >= DateTime.UtcNow)
            .CountAsync(a => !db.AssignmentSubmissions.Any(s => s.AssignmentId == a.Id && s.UserId == id), ct);
        var assessments = await db.Assignments.Where(a => a.IsPublished && enrollments.Contains(a.CourseId) && a.DueAtUtc >= DateTime.UtcNow)
            .OrderBy(a => a.DueAtUtc).Take(5).Select(a => new { a.Id, a.Title, a.DueAtUtc, a.CourseId,
                IsSubmitted = db.AssignmentSubmissions.Any(s => s.AssignmentId == a.Id && s.UserId == id) }).ToListAsync(ct);
        var announcementRows = await db.Announcements.Where(a => a.IsPublished).OrderByDescending(a => a.PublishedAtUtc).Take(5)
            .Select(a => new { a.Id, a.Title, a.Body, a.PublishedAtUtc, IsRead = db.AnnouncementReads.Any(r => r.UserId == id && r.AnnouncementId == a.Id) }).ToListAsync(ct);
        var schedule = await db.ScheduleEvents.Where(e => (e.UserId == null || e.UserId == id) && e.StartsAtUtc >= DateTime.UtcNow)
            .OrderBy(e => e.StartsAtUtc).Take(5).ToListAsync(ct);
        return Ok(new {
            Courses = activeCourses,
            Stats = new { EnrolledCourses = courses.Count, CompletedCourses = activeCourses.Count(c => c.Progress == 100),
                InProgressCourses = activeCourses.Count(c => c.Progress is > 0 and < 100), AccessibleResources = resources,
                LearningHours = Math.Round((await activities.SumAsync(a => (long)a.ProgressSeconds, ct)) / 3600d, 1),
                ReadingStreak = streak, OverallProgress = totalLessons == 0 ? 0 : (int)Math.Round(100d * completedLessons / totalLessons),
                PendingAssignments = pendingAssignments, UpcomingAssessments = assessments.Count, OpenSupportTickets = tickets },
            WeeklyActivity = weekly, ScoreTrend = scoreTrend, TopicPerformance = topicPerformance,
            Recommendations = recommendations,
            RecentLesson = lastLessonActivity, Assessments = assessments, Schedule = schedule,
            Announcements = announcementRows, LastActivityUtc = lastActivity
        });
    }

    [HttpGet("courses")]
    public async Task<IActionResult> Courses(CancellationToken ct)
    {
        var id = UserId;
        var query = db.Courses.AsNoTracking().Where(c => c.IsPublished || IsStaff);
        return Ok(await query.OrderBy(c => c.Title).Select(c => new { c.Id, c.Title, c.Subject, c.Description,
            IsPublished = c.IsPublished, IsEnrolled = db.Enrollments.Any(e => e.UserId == id && e.CourseId == c.Id),
            Lessons = c.Lessons.OrderBy(l => l.Order).Select(l => new { l.Id, l.Title, l.Order, l.DurationMinutes }).ToList() }).ToListAsync(ct));
    }

    [HttpPost("courses/{courseId:guid}/enroll")]
    public async Task<IActionResult> Enroll(Guid courseId, CancellationToken ct)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == courseId && c.IsPublished, ct)) return NotFound();
        if (await db.Enrollments.AnyAsync(e => e.UserId == UserId && e.CourseId == courseId, ct)) return Conflict(new { message = "Already enrolled." });
        db.Enrollments.Add(new Enrollment { UserId = UserId, CourseId = courseId });
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("lessons/{lessonId:guid}")]
    public async Task<IActionResult> Lesson(Guid lessonId, CancellationToken ct)
    {
        var lesson = await db.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId, ct);
        if (lesson is null) return NotFound();
        if (!IsStaff && !await db.Enrollments.AnyAsync(e => e.UserId == UserId && e.CourseId == lesson.CourseId, ct)) return Forbid();
        var latest = await db.LearningActivities.Where(a => a.UserId == UserId && a.LessonId == lessonId).OrderByDescending(a => a.OccurredAtUtc).FirstOrDefaultAsync(ct);
        var isBookmarked = await db.LessonBookmarks.AnyAsync(b => b.UserId == UserId && b.LessonId == lessonId, ct);
        return Ok(new { lesson.Id, lesson.Title, lesson.Content, lesson.Order, lesson.DurationMinutes, lesson.VideoUrl, lesson.TranscriptUrl, CourseTitle = lesson.Course!.Title, ProgressSeconds = latest?.ProgressSeconds ?? 0, MediaPositionSeconds = latest?.MediaPositionSeconds ?? 0, ProgressPercent = latest?.ProgressPercent ?? 0, IsCompleted = latest?.IsCompleted ?? false, IsBookmarked = isBookmarked });
    }

    [HttpPost("lessons/{lessonId:guid}/bookmark")]
    public async Task<IActionResult> BookmarkLesson(Guid lessonId, CancellationToken ct)
    {
        var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId, ct);
        if (lesson is null) return NotFound();
        if (!IsStaff && !await db.Enrollments.AnyAsync(e => e.UserId == UserId && e.CourseId == lesson.CourseId, ct)) return Forbid();
        var existing = await db.LessonBookmarks.FirstOrDefaultAsync(b => b.UserId == UserId && b.LessonId == lessonId, ct);
        if (existing is null) db.LessonBookmarks.Add(new LessonBookmark { UserId = UserId, LessonId = lessonId });
        else db.LessonBookmarks.Remove(existing);
        await db.SaveChangesAsync(ct);
        return Ok(new { IsBookmarked = existing is null });
    }

    public record ActivityRequest(Guid? LessonId, Guid? ResourceId, string Type, int ProgressSeconds, int? Score, bool IsCompleted, string? Topic, int? MediaPositionSeconds = null, int? ProgressPercent = null);
    [HttpPost("activities")]
    public async Task<IActionResult> RecordActivity(ActivityRequest request, CancellationToken ct)
    {
        if (request.ProgressSeconds < 0 || request.ProgressSeconds > 86400 || request.Score is not null || request.MediaPositionSeconds is < 0 or > 86400 || request.ProgressPercent is < 0 or > 100) return BadRequest();
        Guid? courseId = null;
        if (request.LessonId is Guid lessonId)
        {
            var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId, ct);
            if (lesson is null) return NotFound();
            courseId = lesson.CourseId;
            if (!IsStaff && !await db.Enrollments.AnyAsync(e => e.UserId == UserId && e.CourseId == courseId, ct)) return Forbid();
        }
        if (request.ResourceId is Guid resourceId && !await db.LearningResources.AnyAsync(r => r.Id == resourceId && r.IsPublished, ct)) return NotFound();
        db.LearningActivities.Add(new LearningActivity { UserId = UserId, CourseId = courseId, LessonId = request.LessonId,
            ResourceId = request.ResourceId, Type = request.Type.Length <= 40 ? request.Type : "lesson",
            ProgressSeconds = request.ProgressSeconds, MediaPositionSeconds = request.MediaPositionSeconds, ProgressPercent = request.ProgressPercent,
            Score = request.Score, IsCompleted = request.IsCompleted,
            Topic = string.IsNullOrWhiteSpace(request.Topic) ? null : request.Topic[..Math.Min(100, request.Topic.Length)] });
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    [HttpGet("resources")]
    public async Task<IActionResult> Resources([FromQuery] string? q, [FromQuery] string? type, [FromQuery] string? subject,
        [FromQuery] Guid? courseId, [FromQuery] string? category, [FromQuery] string? sort = "recent", [FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.LearningResources.AsNoTracking().Where(r => r.IsPublished || IsStaff);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(r => r.Title.Contains(q) || r.Description.Contains(q) || r.Subject.Contains(q));
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(r => r.Type == type);
        if (!string.IsNullOrWhiteSpace(subject)) query = query.Where(r => r.Subject == subject);
        if (courseId.HasValue) query = query.Where(r => r.CourseId == courseId.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(r => r.Category == category);
        var total = await query.CountAsync(ct);
        query = sort == "title" ? query.OrderBy(r => r.Title) : query.OrderByDescending(r => r.CreatedAtUtc);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(r => new { r.Id, r.Title, r.Description,
            r.Type, r.Category, r.Subject, r.FileUrl, r.FileName, r.TranscriptUrl, r.CourseId, r.CreatedAtUtc, r.IsPublished,
            IsBookmarked = db.ResourceBookmarks.Any(b => b.UserId == UserId && b.ResourceId == r.Id) }).ToListAsync(ct);
        return Ok(new { Items = items, Total = total, Page = page, PageSize = pageSize });
    }

    [HttpPost("resources/{resourceId:guid}/bookmark")]
    public async Task<IActionResult> Bookmark(Guid resourceId, CancellationToken ct)
    {
        if (!await db.LearningResources.AnyAsync(r => r.Id == resourceId && r.IsPublished, ct)) return NotFound();
        var existing = await db.ResourceBookmarks.FirstOrDefaultAsync(b => b.UserId == UserId && b.ResourceId == resourceId, ct);
        if (existing is null) db.ResourceBookmarks.Add(new ResourceBookmark { UserId = UserId, ResourceId = resourceId });
        else db.ResourceBookmarks.Remove(existing);
        await db.SaveChangesAsync(ct);
        return Ok(new { IsBookmarked = existing is null });
    }

    [HttpGet("bookmarks")]
    public async Task<IActionResult> Bookmarks(CancellationToken ct)
    {
        var resources = await db.ResourceBookmarks.Where(b => b.UserId == UserId)
            .Join(db.LearningResources, b => b.ResourceId, r => r.Id, (b, r) => new { r.Id, r.Title, Type = r.Type, r.FileUrl }).ToListAsync(ct);
        var lessons = await db.LessonBookmarks.Where(b => b.UserId == UserId)
            .Join(db.Lessons, b => b.LessonId, l => l.Id, (b, l) => new { l.Id, l.Title, Type = "Lesson", FileUrl = (string?)null }).ToListAsync(ct);
        return Ok(new { Resources = resources, Lessons = lessons });
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> Preferences(CancellationToken ct) => Ok(await db.AccessibilityPreferences.FindAsync([UserId], ct)
        ?? new AccessibilityPreference { UserId = UserId });

    [HttpPut("preferences")]
    public async Task<IActionResult> SavePreferences(AccessibilityPreference value, CancellationToken ct)
    {
        if (value.FontSize is < 80 or > 150 || value.LineSpacing is < 1 or > 3 || !new[] { "default", "dyslexia", "mono" }.Contains(value.FontFamily) || !new[] { "paper", "light", "dark", "high-contrast" }.Contains(value.Theme)) return BadRequest();
        var current = await db.AccessibilityPreferences.FindAsync([UserId], ct);
        if (current is null) { value.UserId = UserId; db.AccessibilityPreferences.Add(value); }
        else { current.FontSize = value.FontSize; current.FontFamily = value.FontFamily; current.Theme = value.Theme; current.LineSpacing = value.LineSpacing; current.ReduceMotion = value.ReduceMotion; current.UnderlineLinks = value.UnderlineLinks; current.ReadAloud = value.ReadAloud; }
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("announcements/{id:guid}/read")]
    public async Task<IActionResult> MarkAnnouncementRead(Guid id, CancellationToken ct)
    {
        if (!await db.Announcements.AnyAsync(a => a.Id == id && a.IsPublished, ct)) return NotFound();
        if (!await db.AnnouncementReads.AnyAsync(r => r.UserId == UserId && r.AnnouncementId == id, ct))
            db.AnnouncementReads.Add(new AnnouncementRead { UserId = UserId, AnnouncementId = id });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("assignments/{id:guid}/submit")]
    public async Task<IActionResult> SubmitAssignment(Guid id, [FromBody] string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 10000) return BadRequest();
        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == id && a.IsPublished, ct);
        if (assignment is null) return NotFound();
        if (assignment.DueAtUtc < DateTime.UtcNow || !await db.Enrollments.AnyAsync(e => e.UserId == UserId && e.CourseId == assignment.CourseId, ct)) return Forbid();
        var submission = await db.AssignmentSubmissions.FirstOrDefaultAsync(s => s.UserId == UserId && s.AssignmentId == id, ct);
        if (submission is null) db.AssignmentSubmissions.Add(new AssignmentSubmission { UserId = UserId, AssignmentId = id, SubmissionText = text.Trim() });
        else { submission.SubmissionText = text.Trim(); submission.SubmittedAtUtc = DateTime.UtcNow; }
        await db.SaveChangesAsync(ct);
        return Ok();
    }

    public record QuizQuestionInput(string Question, string[] Options, int CorrectOption);
    public record QuizQuestionView(string Question, string[] Options);
    [HttpGet("courses/{courseId:guid}/quizzes")]
    public async Task<IActionResult> CourseQuizzes(Guid courseId, CancellationToken ct)
    {
        if (!IsStaff && !await db.Enrollments.AnyAsync(e => e.UserId == UserId && e.CourseId == courseId, ct)) return Forbid();
        return Ok(await db.Quizzes.Where(q => q.CourseId == courseId && (q.IsPublished || IsStaff))
            .Select(q => new { q.Id, q.Title, q.Topic, Attempts = db.QuizAttempts.Count(a => a.UserId == UserId && a.QuizId == q.Id) }).ToListAsync(ct));
    }

    [HttpGet("quizzes/{id:guid}")]
    public async Task<IActionResult> QuizDetails(Guid id, CancellationToken ct)
    {
        var quiz = await db.Quizzes.FindAsync([id], ct); if (quiz is null || (!quiz.IsPublished && !IsStaff)) return NotFound();
        if (!IsStaff && !await db.Enrollments.AnyAsync(e => e.UserId == UserId && e.CourseId == quiz.CourseId, ct)) return Forbid();
        var questions = System.Text.Json.JsonSerializer.Deserialize<List<QuizQuestionInput>>(quiz.QuestionsJson) ?? [];
        return Ok(new { quiz.Id, quiz.Title, quiz.Topic, Questions = questions.Select(q => new QuizQuestionView(q.Question, q.Options)) });
    }

    public record QuizAnswersInput(int[] Answers);
    [HttpPost("quizzes/{id:guid}/attempts")]
    public async Task<IActionResult> SubmitQuiz(Guid id, QuizAnswersInput input, CancellationToken ct)
    {
        var quiz = await db.Quizzes.FindAsync([id], ct); if (quiz is null || !quiz.IsPublished) return NotFound();
        if (!await db.Enrollments.AnyAsync(e => e.UserId == UserId && e.CourseId == quiz.CourseId, ct)) return Forbid();
        var questions = System.Text.Json.JsonSerializer.Deserialize<List<QuizQuestionInput>>(quiz.QuestionsJson) ?? [];
        if (questions.Count == 0 || input.Answers.Length != questions.Count || Enumerable.Range(0, input.Answers.Length).Any(index => input.Answers[index] < 0 || input.Answers[index] >= questions[index].Options.Length)) return BadRequest();
        var score = (int)Math.Round(100d * questions.Select((q, index) => q.CorrectOption == input.Answers[index]).Count(correct => correct) / questions.Count);
        var attempt = new QuizAttempt { UserId = UserId, QuizId = id, Score = score };
        db.QuizAttempts.Add(attempt);
        db.LearningActivities.Add(new LearningActivity { UserId = UserId, CourseId = quiz.CourseId, Type = "quiz", Score = score, Topic = quiz.Topic });
        await db.SaveChangesAsync(ct);
        return Ok(new { Score = score, TotalQuestions = questions.Count, AttemptedAtUtc = attempt.AttemptedAtUtc });
    }

    public record AssignmentInput(string Title, string Description, DateTime DueAtUtc, Guid CourseId, bool IsPublished);
    [HttpPost("admin/assignments")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> CreateAssignment(AssignmentInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 180 || input.Description?.Length > 3000 || input.DueAtUtc <= DateTime.UtcNow || !await db.Courses.AnyAsync(c => c.Id == input.CourseId, ct)) return BadRequest();
        var item = new Assignment { CourseId = input.CourseId, Title = input.Title.Trim(), Description = input.Description ?? "", DueAtUtc = input.DueAtUtc, IsPublished = input.IsPublished };
        db.Assignments.Add(item); await db.SaveChangesAsync(ct); return Created($"/api/learning/assignments/{item.Id}", item);
    }

    public record QuizInput(string Title, string Topic, Guid CourseId, List<QuizQuestionInput> Questions, bool IsPublished);
    [HttpPost("admin/quizzes")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> CreateQuiz(QuizInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 180 || string.IsNullOrWhiteSpace(input.Topic) || input.Topic.Length > 100 || input.Questions.Count is < 1 or > 100 ||
            input.Questions.Any(q => string.IsNullOrWhiteSpace(q.Question) || q.Options.Length < 2 || q.CorrectOption < 0 || q.CorrectOption >= q.Options.Length) ||
            !await db.Courses.AnyAsync(c => c.Id == input.CourseId, ct)) return BadRequest();
        var questionsJson = System.Text.Json.JsonSerializer.Serialize(input.Questions);
        if (questionsJson.Length > 30000) return BadRequest();
        var quiz = new Quiz { CourseId = input.CourseId, Title = input.Title.Trim(), Topic = input.Topic.Trim(),
            QuestionsJson = questionsJson, IsPublished = input.IsPublished };
        db.Quizzes.Add(quiz); await db.SaveChangesAsync(ct); return Created($"/api/learning/quizzes/{quiz.Id}", new { quiz.Id, quiz.Title });
    }

    public record CourseInput(string Title, string Subject, string Description, bool IsPublished);
    [HttpPost("admin/courses")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> CreateCourse(CourseInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160 || string.IsNullOrWhiteSpace(input.Subject) || input.Subject.Length > 100 || input.Description?.Length > 2000) return BadRequest();
        var course = new Course { Title = input.Title.Trim(), Subject = input.Subject.Trim(), Description = input.Description?.Trim() ?? "", IsPublished = input.IsPublished };
        db.Courses.Add(course); await db.SaveChangesAsync(ct); return Created($"/api/learning/courses/{course.Id}", course);
    }

    [HttpPut("admin/courses/{id:guid}")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> UpdateCourse(Guid id, CourseInput input, CancellationToken ct)
    {
        var course = await db.Courses.FindAsync([id], ct); if (course is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160 || string.IsNullOrWhiteSpace(input.Subject) || input.Subject.Length > 100 || input.Description?.Length > 2000) return BadRequest();
        course.Title = input.Title.Trim(); course.Subject = input.Subject.Trim(); course.Description = input.Description?.Trim() ?? ""; course.IsPublished = input.IsPublished;
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("admin/courses/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCourse(Guid id, CancellationToken ct)
    {
        var course = await db.Courses.FindAsync([id], ct); if (course is null) return NotFound();
        db.LearningActivities.RemoveRange(await db.LearningActivities.Where(a => a.CourseId == id).ToListAsync(ct));
        db.Courses.Remove(course); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPut("admin/lessons/{id:guid}")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> UpdateLesson(Guid id, LessonInput input, CancellationToken ct)
    {
        var lesson = await db.Lessons.FindAsync([id], ct); if (lesson is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160 || input.Content?.Length > 20000 || input.DurationMinutes < 0 || input.Order < 0 || (input.VideoUrl is not null && !IsSafeContentUrl(input.VideoUrl)) || (input.TranscriptUrl is not null && !IsSafeContentUrl(input.TranscriptUrl))) return BadRequest();
        lesson.Title = input.Title.Trim(); lesson.Content = input.Content ?? ""; lesson.Order = input.Order; lesson.DurationMinutes = input.DurationMinutes;
        lesson.VideoUrl = input.VideoUrl; lesson.TranscriptUrl = input.TranscriptUrl; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("admin/lessons/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLesson(Guid id, CancellationToken ct)
    {
        var lesson = await db.Lessons.FindAsync([id], ct); if (lesson is null) return NotFound();
        db.LearningActivities.RemoveRange(await db.LearningActivities.Where(a => a.LessonId == id).ToListAsync(ct));
        db.Lessons.Remove(lesson); await db.SaveChangesAsync(ct); return NoContent();
    }

    public record LessonInput(string Title, string Content, int Order, int DurationMinutes, string? VideoUrl, string? TranscriptUrl);
    [HttpPost("admin/courses/{courseId:guid}/lessons")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> CreateLesson(Guid courseId, LessonInput input, CancellationToken ct)
    {
        if (!await db.Courses.AnyAsync(c => c.Id == courseId, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 160 || input.Content?.Length > 20000 || input.DurationMinutes < 0 || input.Order < 0 || (input.VideoUrl is not null && !IsSafeContentUrl(input.VideoUrl)) || (input.TranscriptUrl is not null && !IsSafeContentUrl(input.TranscriptUrl))) return BadRequest();
        var lesson = new Lesson { CourseId = courseId, Title = input.Title.Trim(), Content = input.Content ?? "", Order = input.Order,
            DurationMinutes = input.DurationMinutes, VideoUrl = input.VideoUrl, TranscriptUrl = input.TranscriptUrl };
        db.Lessons.Add(lesson); await db.SaveChangesAsync(ct); return Created($"/api/learning/lessons/{lesson.Id}", lesson);
    }

    public record ResourceInput(string Title, string Description, string Type, string Category, string Subject, string? FileUrl, string? FileName, string? TranscriptUrl, Guid? CourseId, bool IsPublished);
    [HttpPost("admin/resources")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> CreateResource(ResourceInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 || string.IsNullOrWhiteSpace(input.Type) || input.Type.Length > 60 || input.Description?.Length > 1000 || input.Category?.Length > 100 || input.Subject?.Length > 100 || input.FileName?.Length > 260 || !IsSafeContentUrl(input.FileUrl) || (input.TranscriptUrl is not null && !IsSafeContentUrl(input.TranscriptUrl))) return BadRequest();
        if (input.CourseId is Guid courseId && !await db.Courses.AnyAsync(c => c.Id == courseId, ct)) return BadRequest();
        var r = new LearningResource { Title = input.Title.Trim(), Description = input.Description ?? "", Type = input.Type,
            Category = input.Category ?? "", Subject = input.Subject ?? "", FileUrl = input.FileUrl, FileName = input.FileName,
            TranscriptUrl = input.TranscriptUrl, CourseId = input.CourseId, IsPublished = input.IsPublished };
        db.LearningResources.Add(r); await db.SaveChangesAsync(ct); return Created($"/api/learning/resources/{r.Id}", r);
    }

    [HttpPut("admin/resources/{id:guid}")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> UpdateResource(Guid id, ResourceInput input, CancellationToken ct)
    {
        var r = await db.LearningResources.FindAsync([id], ct); if (r is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 200 || string.IsNullOrWhiteSpace(input.Type) || input.Type.Length > 60 || input.Description?.Length > 1000 || input.Category?.Length > 100 || input.Subject?.Length > 100 || input.FileName?.Length > 260 || !IsSafeContentUrl(input.FileUrl) || (input.TranscriptUrl is not null && !IsSafeContentUrl(input.TranscriptUrl))) return BadRequest();
        r.Title = input.Title.Trim(); r.Description = input.Description ?? ""; r.Type = input.Type; r.Category = input.Category ?? "";
        r.Subject = input.Subject ?? ""; r.FileUrl = input.FileUrl; r.FileName = input.FileName; r.TranscriptUrl = input.TranscriptUrl;
        r.CourseId = input.CourseId; r.IsPublished = input.IsPublished; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("admin/resources/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteResource(Guid id, CancellationToken ct)
    {
        var r = await db.LearningResources.FindAsync([id], ct); if (r is null) return NotFound();
        db.LearningActivities.RemoveRange(await db.LearningActivities.Where(a => a.ResourceId == id).ToListAsync(ct));
        db.LearningResources.Remove(r); await db.SaveChangesAsync(ct); return NoContent();
    }

    public record AnnouncementInput(string Title, string Body, bool IsPublished);
    [HttpPost("admin/announcements")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAnnouncement(AnnouncementInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 180 || string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 3000) return BadRequest();
        var item = new Announcement { Title = input.Title.Trim(), Body = input.Body.Trim(), IsPublished = input.IsPublished };
        db.Announcements.Add(item); await db.SaveChangesAsync(ct); return Created($"/api/learning/announcements/{item.Id}", item);
    }

    [HttpPut("admin/announcements/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAnnouncement(Guid id, AnnouncementInput input, CancellationToken ct)
    {
        var item = await db.Announcements.FindAsync([id], ct); if (item is null) return NotFound();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 180 || string.IsNullOrWhiteSpace(input.Body) || input.Body.Length > 3000) return BadRequest();
        item.Title = input.Title.Trim(); item.Body = input.Body.Trim(); item.IsPublished = input.IsPublished; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("admin/announcements/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id, CancellationToken ct)
    {
        var item = await db.Announcements.FindAsync([id], ct); if (item is null) return NotFound();
        db.Announcements.Remove(item); await db.SaveChangesAsync(ct); return NoContent();
    }

    public record ScheduleInput(string Title, string Type, DateTime StartsAtUtc, DateTime? EndsAtUtc, string? AccessNotes, string? UserId, Guid? CourseId);
    [HttpPost("admin/schedule")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> CreateSchedule(ScheduleInput input, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 180 || string.IsNullOrWhiteSpace(input.Type) || input.Type.Length > 40 || input.AccessNotes?.Length > 500 || input.StartsAtUtc < DateTime.UtcNow.AddYears(-1) || (input.EndsAtUtc is DateTime ends && ends <= input.StartsAtUtc)) return BadRequest();
        if (input.UserId is not null && !await db.Users.AnyAsync(u => u.Id == input.UserId, ct)) return BadRequest();
        if (input.CourseId is Guid scheduledCourse && !await db.Courses.AnyAsync(c => c.Id == scheduledCourse, ct)) return BadRequest();
        var item = new ScheduleEvent { Title = input.Title.Trim(), Type = input.Type, StartsAtUtc = input.StartsAtUtc,
            EndsAtUtc = input.EndsAtUtc, AccessNotes = input.AccessNotes, UserId = input.UserId, CourseId = input.CourseId };
        db.ScheduleEvents.Add(item); await db.SaveChangesAsync(ct); return Created($"/api/learning/schedule/{item.Id}", item);
    }

    [HttpGet("admin/users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users(CancellationToken ct) => Ok(await db.Users.AsNoTracking()
        .Select(u => new { u.Id, u.FullName, u.Email, u.Role, u.IsActive, EnrolledCourses = db.Enrollments.Count(e => e.UserId == u.Id) }).ToListAsync(ct));

    [HttpGet("admin/overview")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminOverview(CancellationToken ct) => Ok(new {
        Users = await db.Users.CountAsync(ct), Students = await db.Users.CountAsync(u => u.Role == Sadachar.Core.Enums.UserRole.Student, ct),
        Courses = await db.Courses.CountAsync(ct), PublishedResources = await db.LearningResources.CountAsync(r => r.IsPublished, ct),
        Enrollments = await db.Enrollments.CountAsync(ct), PendingAssignments = await db.Assignments.CountAsync(a => a.IsPublished && a.DueAtUtc >= DateTime.UtcNow, ct),
        OpenTickets = await db.GrievanceTickets.CountAsync(t => t.Status == Sadachar.Core.Enums.TicketStatus.Open || t.Status == Sadachar.Core.Enums.TicketStatus.InProgress, ct)
    });

    [HttpGet("admin/analytics")]
    [Authorize(Roles = "Admin,Educator")]
    public async Task<IActionResult> AdminAnalytics(CancellationToken ct)
    {
        var enrollments = await db.Courses.Select(c => new { c.Id, c.Title, Total = db.Enrollments.Count(e => e.CourseId == c.Id) }).ToListAsync(ct);
        var scores = await db.QuizAttempts.Join(db.Quizzes, a => a.QuizId, q => q.Id, (a, q) => new { a.UserId, a.Score, a.AttemptedAtUtc, q.Topic, q.Title })
            .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new { a.Title, a.Topic, a.Score, a.AttemptedAtUtc, Student = u.FullName })
            .OrderByDescending(a => a.AttemptedAtUtc).Take(100).ToListAsync(ct);
        return Ok(new { Enrollments = enrollments, Scores = scores });
    }

    [HttpPut("admin/users/{id}/active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetUserActive(string id, [FromBody] bool isActive, CancellationToken ct)
    {
        if (id == UserId) return BadRequest(new { message = "You cannot deactivate your own account." });
        var user = await db.Users.FindAsync([id], ct); if (user is null) return NotFound();
        user.IsActive = isActive; user.SecurityStamp = Guid.NewGuid().ToString(); await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPost("admin/upload")]
    [Authorize(Roles = "Admin,Educator")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > 25_000_000) return BadRequest();
        var extension = Path.GetExtension(file.FileName);
        var allowed = new[] { ".pdf", ".epub", ".mp3", ".mp4", ".txt", ".docx", ".vtt", ".srt" };
        if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Unsupported resource file type." });
        var folder = Path.Combine(environment.WebRootPath, "uploads", "resources"); Directory.CreateDirectory(folder);
        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await using (var stream = System.IO.File.Create(Path.Combine(folder, storedName))) await file.CopyToAsync(stream, ct);
        return Ok(new { FileUrl = $"/uploads/resources/{storedName}", FileName = Path.GetFileName(file.FileName) });
    }
}

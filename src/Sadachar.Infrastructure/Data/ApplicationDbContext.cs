using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;

namespace Sadachar.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ComplianceLog> ComplianceLogs => Set<ComplianceLog>();
    public DbSet<GrievanceTicket> GrievanceTickets => Set<GrievanceTicket>();
    public DbSet<ConductLog> ConductLogs => Set<ConductLog>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LearningResource> LearningResources => Set<LearningResource>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<LearningActivity> LearningActivities => Set<LearningActivity>();
    public DbSet<ResourceBookmark> ResourceBookmarks => Set<ResourceBookmark>();
    public DbSet<LessonBookmark> LessonBookmarks => Set<LessonBookmark>();
    public DbSet<AccessibilityPreference> AccessibilityPreferences => Set<AccessibilityPreference>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementRead> AnnouncementReads => Set<AnnouncementRead>();
    public DbSet<ScheduleEvent> ScheduleEvents => Set<ScheduleEvent>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ComplianceLog Configurations
        builder.Entity<ComplianceLog>(entity =>
        {
            entity.HasIndex(e => e.MaterialId);
            entity.HasIndex(e => e.AuditedAt);
            entity.Property(e => e.WcagScore).HasPrecision(5, 2);
        });

        // GrievanceTicket Configurations & Indexes for fast SLA polling
        builder.Entity<GrievanceTicket>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SLAExpiryDateTime);
            entity.HasOne(e => e.Student)
                  .WithMany(u => u.GrievanceTickets)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ConductLog Configurations
        builder.Entity<ConductLog>(entity =>
        {
            entity.HasIndex(e => e.PseudonymizedUserHash);
            entity.HasIndex(e => e.DataRetentionExpiryUtc);
            entity.Property(e => e.AiConfidenceScore).HasPrecision(4, 3);
        });

        builder.Entity<Course>().HasIndex(x => x.IsPublished);
        builder.Entity<AccessibilityPreference>().Property(x => x.LineSpacing).HasPrecision(3, 2);
        builder.Entity<Lesson>().HasOne(x => x.Course).WithMany(x => x.Lessons).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LearningResource>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Enrollment>().HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
        builder.Entity<Enrollment>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Enrollment>().HasOne(x => x.Course).WithMany(x => x.Enrollments).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LearningActivity>().HasIndex(x => new { x.UserId, x.OccurredAtUtc });
        builder.Entity<LearningActivity>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LearningActivity>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<LearningActivity>().HasOne<Lesson>().WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<LearningActivity>().HasOne<LearningResource>().WithMany().HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<ResourceBookmark>().HasIndex(x => new { x.UserId, x.ResourceId }).IsUnique();
        builder.Entity<ResourceBookmark>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ResourceBookmark>().HasOne<LearningResource>().WithMany().HasForeignKey(x => x.ResourceId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LessonBookmark>().HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();
        builder.Entity<LessonBookmark>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LessonBookmark>().HasOne<Lesson>().WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AccessibilityPreference>().HasKey(x => x.UserId);
        builder.Entity<AnnouncementRead>().HasKey(x => new { x.UserId, x.AnnouncementId });
        builder.Entity<AnnouncementRead>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AnnouncementRead>().HasOne<Announcement>().WithMany().HasForeignKey(x => x.AnnouncementId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ScheduleEvent>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<ScheduleEvent>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Assignment>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssignmentSubmission>().HasIndex(x => new { x.UserId, x.AssignmentId }).IsUnique();
        builder.Entity<AssignmentSubmission>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssignmentSubmission>().HasOne<Assignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Quiz>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<QuizAttempt>().HasIndex(x => new { x.UserId, x.AttemptedAtUtc });
        builder.Entity<QuizAttempt>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<QuizAttempt>().HasOne<Quiz>().WithMany().HasForeignKey(x => x.QuizId).OnDelete(DeleteBehavior.Cascade);
    }
}


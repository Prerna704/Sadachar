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
    public DbSet<AccessibilityProfile> AccessibilityProfiles => Set<AccessibilityProfile>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialFormat> MaterialFormats => Set<MaterialFormat>();
    public DbSet<ComplianceAudit> ComplianceAudits => Set<ComplianceAudit>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LearningResource> LearningResources => Set<LearningResource>();
    public DbSet<ResourceCategory> ResourceCategories => Set<ResourceCategory>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();
    public DbSet<VolunteerStudentAssignment> VolunteerStudentAssignments => Set<VolunteerStudentAssignment>();
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
        builder.Entity<ResourceCategory>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<AccessibilityPreference>().Property(x => x.LineSpacing).HasPrecision(3, 2);
        builder.Entity<Lesson>().HasOne(x => x.Course).WithMany(x => x.Lessons).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LearningResource>().HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Enrollment>().HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
        builder.Entity<Enrollment>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Enrollment>().HasOne(x => x.Course).WithMany(x => x.Enrollments).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ParentStudentLink>().HasIndex(x => new { x.ParentUserId, x.StudentUserId }).IsUnique();
        builder.Entity<ParentStudentLink>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ParentUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<ParentStudentLink>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<VolunteerStudentAssignment>().HasIndex(x => new { x.VolunteerUserId, x.StudentUserId }).IsUnique();
        builder.Entity<VolunteerStudentAssignment>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.VolunteerUserId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<VolunteerStudentAssignment>().HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StudentUserId).OnDelete(DeleteBehavior.NoAction);
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

        // AccessEd ER Configurations
        builder.Entity<AccessibilityProfile>(entity =>
        {
            entity.HasKey(e => e.ProfileId);
            entity.HasOne(e => e.User)
                  .WithOne(u => u.AccessibilityProfile)
                  .HasForeignKey<AccessibilityProfile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Course>(entity =>
        {
            entity.HasOne(e => e.Teacher)
                  .WithMany(u => u.TaughtCourses)
                  .HasForeignKey(e => e.TeacherId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId);
            entity.HasOne(e => e.Course)
                  .WithMany(c => c.Materials)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MaterialFormat>(entity =>
        {
            entity.HasKey(e => e.FormatId);
            entity.HasOne(e => e.Material)
                  .WithMany(m => m.Formats)
                  .HasForeignKey(e => e.MaterialId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ComplianceAudit>(entity =>
        {
            entity.HasKey(e => e.AuditId);
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.Standard);
        });

        builder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.TicketId);
            entity.HasIndex(e => e.TicketNumber).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Tickets)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Assignee)
                  .WithMany()
                  .HasForeignKey(e => e.AssigneeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<TicketComment>(entity =>
        {
            entity.HasKey(e => e.CommentId);
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.Comments)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Author)
                  .WithMany(u => u.TicketComments)
                  .HasForeignKey(e => e.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}


using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sadachar.Infrastructure.Data;

#nullable disable

namespace Sadachar.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261106123000_AddLearningManagement")]
public partial class AddLearningManagement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("Courses", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
            Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
            IsPublished = table.Column<bool>(type: "bit", nullable: false),
            CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Courses", x => x.Id));

        migrationBuilder.CreateTable("Announcements", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
            Body = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
            PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            IsPublished = table.Column<bool>(type: "bit", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Announcements", x => x.Id));

        migrationBuilder.CreateTable("AccessibilityPreferences", table => new
        {
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            FontSize = table.Column<int>(type: "int", nullable: false),
            FontFamily = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
            Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
            LineSpacing = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: false),
            ReduceMotion = table.Column<bool>(type: "bit", nullable: false),
            UnderlineLinks = table.Column<bool>(type: "bit", nullable: false),
            ReadAloud = table.Column<bool>(type: "bit", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AccessibilityPreferences", x => x.UserId);
            table.ForeignKey("FK_AccessibilityPreferences_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("Lessons", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
            Content = table.Column<string>(type: "nvarchar(20000)", maxLength: 20000, nullable: false),
            Order = table.Column<int>(type: "int", nullable: false),
            DurationMinutes = table.Column<int>(type: "int", nullable: false),
            VideoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
            TranscriptUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Lessons", x => x.Id);
            table.ForeignKey("FK_Lessons_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("LearningResources", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            Type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
            Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
            FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
            TranscriptUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
            IsPublished = table.Column<bool>(type: "bit", nullable: false),
            CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_LearningResources", x => x.Id);
            table.ForeignKey("FK_LearningResources_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.SetNull);
        });

        migrationBuilder.CreateTable("Enrollments", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
            CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            EnrolledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Enrollments", x => x.Id);
            table.ForeignKey("FK_Enrollments_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_Enrollments_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("LearningActivities", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
            ProgressSeconds = table.Column<int>(type: "int", nullable: false),
            MediaPositionSeconds = table.Column<int>(type: "int", nullable: true),
            ProgressPercent = table.Column<int>(type: "int", nullable: true),
            Score = table.Column<int>(type: "int", nullable: true),
            IsCompleted = table.Column<bool>(type: "bit", nullable: false),
            Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
            OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_LearningActivities", x => x.Id);
            table.ForeignKey("FK_LearningActivities_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_LearningActivities_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.NoAction);
            table.ForeignKey("FK_LearningActivities_Lessons_LessonId", x => x.LessonId, "Lessons", "Id", onDelete: ReferentialAction.NoAction);
            table.ForeignKey("FK_LearningActivities_LearningResources_ResourceId", x => x.ResourceId, "LearningResources", "Id", onDelete: ReferentialAction.NoAction);
        });

        migrationBuilder.CreateTable("ResourceBookmarks", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ResourceBookmarks", x => x.Id);
            table.ForeignKey("FK_ResourceBookmarks_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_ResourceBookmarks_LearningResources_ResourceId", x => x.ResourceId, "LearningResources", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("LessonBookmarks", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_LessonBookmarks", x => x.Id);
            table.ForeignKey("FK_LessonBookmarks_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_LessonBookmarks_Lessons_LessonId", x => x.LessonId, "Lessons", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("AnnouncementReads", table => new
        {
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            AnnouncementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AnnouncementReads", x => new { x.UserId, x.AnnouncementId });
            table.ForeignKey("FK_AnnouncementReads_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_AnnouncementReads_Announcements_AnnouncementId", x => x.AnnouncementId, "Announcements", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("ScheduleEvents", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
            CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
            Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
            AccessNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
            StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ScheduleEvents", x => x.Id);
            table.ForeignKey("FK_ScheduleEvents_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.SetNull);
            table.ForeignKey("FK_ScheduleEvents_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.SetNull);
        });

        migrationBuilder.CreateTable("Assignments", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
            Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
            DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            IsPublished = table.Column<bool>(type: "bit", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Assignments", x => x.Id);
            table.ForeignKey("FK_Assignments_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("Quizzes", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
            Topic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            QuestionsJson = table.Column<string>(type: "nvarchar(30000)", maxLength: 30000, nullable: false),
            IsPublished = table.Column<bool>(type: "bit", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Quizzes", x => x.Id);
            table.ForeignKey("FK_Quizzes_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("AssignmentSubmissions", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            SubmissionText = table.Column<string>(type: "nvarchar(10000)", maxLength: 10000, nullable: false),
            SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            Score = table.Column<int>(type: "int", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_AssignmentSubmissions", x => x.Id);
            table.ForeignKey("FK_AssignmentSubmissions_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_AssignmentSubmissions_Assignments_AssignmentId", x => x.AssignmentId, "Assignments", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("QuizAttempts", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            QuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Score = table.Column<int>(type: "int", nullable: false),
            AttemptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_QuizAttempts", x => x.Id);
            table.ForeignKey("FK_QuizAttempts_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_QuizAttempts_Quizzes_QuizId", x => x.QuizId, "Quizzes", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateIndex("IX_Lessons_CourseId", "Lessons", "CourseId");
        migrationBuilder.CreateIndex("IX_Enrollments_CourseId", "Enrollments", "CourseId");
        migrationBuilder.CreateIndex("IX_Enrollments_UserId_CourseId", "Enrollments", new[] { "UserId", "CourseId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Courses_IsPublished", "Courses", "IsPublished");
        migrationBuilder.CreateIndex("IX_LearningResources_CourseId", "LearningResources", "CourseId");
        migrationBuilder.CreateIndex("IX_LearningActivities_UserId_OccurredAtUtc", "LearningActivities", new[] { "UserId", "OccurredAtUtc" });
        migrationBuilder.CreateIndex("IX_LearningActivities_CourseId", "LearningActivities", "CourseId");
        migrationBuilder.CreateIndex("IX_LearningActivities_LessonId", "LearningActivities", "LessonId");
        migrationBuilder.CreateIndex("IX_LearningActivities_ResourceId", "LearningActivities", "ResourceId");
        migrationBuilder.CreateIndex("IX_ResourceBookmarks_UserId_ResourceId", "ResourceBookmarks", new[] { "UserId", "ResourceId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ResourceBookmarks_ResourceId", "ResourceBookmarks", "ResourceId");
        migrationBuilder.CreateIndex("IX_LessonBookmarks_UserId_LessonId", "LessonBookmarks", new[] { "UserId", "LessonId" }, unique: true);
        migrationBuilder.CreateIndex("IX_LessonBookmarks_LessonId", "LessonBookmarks", "LessonId");
        migrationBuilder.CreateIndex("IX_AnnouncementReads_AnnouncementId", "AnnouncementReads", "AnnouncementId");
        migrationBuilder.CreateIndex("IX_ScheduleEvents_UserId", "ScheduleEvents", "UserId");
        migrationBuilder.CreateIndex("IX_ScheduleEvents_CourseId", "ScheduleEvents", "CourseId");
        migrationBuilder.CreateIndex("IX_Assignments_CourseId", "Assignments", "CourseId");
        migrationBuilder.CreateIndex("IX_AssignmentSubmissions_UserId_AssignmentId", "AssignmentSubmissions", new[] { "UserId", "AssignmentId" }, unique: true);
        migrationBuilder.CreateIndex("IX_AssignmentSubmissions_AssignmentId", "AssignmentSubmissions", "AssignmentId");
        migrationBuilder.CreateIndex("IX_Quizzes_CourseId", "Quizzes", "CourseId");
        migrationBuilder.CreateIndex("IX_QuizAttempts_UserId_AttemptedAtUtc", "QuizAttempts", new[] { "UserId", "AttemptedAtUtc" });
        migrationBuilder.CreateIndex("IX_QuizAttempts_QuizId", "QuizAttempts", "QuizId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AccessibilityPreferences");
        migrationBuilder.DropTable("AnnouncementReads");
        migrationBuilder.DropTable("Announcements");
        migrationBuilder.DropTable("AssignmentSubmissions");
        migrationBuilder.DropTable("QuizAttempts");
        migrationBuilder.DropTable("Quizzes");
        migrationBuilder.DropTable("Assignments");
        migrationBuilder.DropTable("Enrollments");
        migrationBuilder.DropTable("LearningActivities");
        migrationBuilder.DropTable("LearningResources");
        migrationBuilder.DropTable("LessonBookmarks");
        migrationBuilder.DropTable("ResourceBookmarks");
        migrationBuilder.DropTable("ScheduleEvents");
        migrationBuilder.DropTable("Lessons");
        migrationBuilder.DropTable("Courses");
    }
}

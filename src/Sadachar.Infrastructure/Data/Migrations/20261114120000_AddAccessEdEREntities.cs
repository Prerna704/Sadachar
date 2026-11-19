using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sadachar.Infrastructure.Data.Migrations
{
    public partial class AddAccessEdEREntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Columns for AspNetUsers
            migrationBuilder.AddColumn<string>(
                name: "AccessibilityNeed",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // 2. Columns for Courses
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Courses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "General");

            migrationBuilder.AddColumn<string>(
                name: "TeacherId",
                table: "Courses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccessibilityScore",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<string>(
                name: "ActiveTerm",
                table: "Courses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Autumn term");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_TeacherId",
                table: "Courses",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_AspNetUsers_TeacherId",
                table: "Courses",
                column: "TeacherId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // 3. AccessibilityProfiles Table
            migrationBuilder.CreateTable(
                name: "AccessibilityProfiles",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FontSize = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TtsSpeed = table.Column<double>(type: "float", nullable: false),
                    Typeface = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LineSpacing = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ReduceMotion = table.Column<bool>(type: "bit", nullable: false),
                    VisibleFocus = table.Column<bool>(type: "bit", nullable: false),
                    UnderlineLinks = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessibilityProfiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_AccessibilityProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessibilityProfiles_UserId",
                table: "AccessibilityProfiles",
                column: "UserId",
                unique: true);

            // 4. Materials Table
            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    AccessibilityScore = table.Column<int>(type: "int", nullable: false),
                    ContentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.MaterialId);
                    table.ForeignKey(
                        name: "FK_Materials_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Materials_CourseId",
                table: "Materials",
                column: "CourseId");

            // 5. MaterialFormats Table
            migrationBuilder.CreateTable(
                name: "MaterialFormats",
                columns: table => new
                {
                    FormatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FormatType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HasCaptions = table.Column<bool>(type: "bit", nullable: false),
                    HasAltText = table.Column<bool>(type: "bit", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialFormats", x => x.FormatId);
                    table.ForeignKey(
                        name: "FK_MaterialFormats_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialFormats_MaterialId",
                table: "MaterialFormats",
                column: "MaterialId");

            // 6. ComplianceAudits Table
            migrationBuilder.CreateTable(
                name: "ComplianceAudits",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Standard = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    AuditedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FlaggedItemsCount = table.Column<int>(type: "int", nullable: false),
                    AuditorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceAudits", x => x.AuditId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAudits_Department",
                table: "ComplianceAudits",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAudits_Standard",
                table: "ComplianceAudits",
                column: "Standard");

            // 7. Tickets Table
            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssigneeId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssigneeInitials = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlaExpiryUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlaDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_Tickets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_AspNetUsers_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TicketNumber",
                table: "Tickets",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_UserId",
                table: "Tickets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssigneeId",
                table: "Tickets",
                column: "AssigneeId");

            // 8. TicketComments Table
            migrationBuilder.CreateTable(
                name: "TicketComments",
                columns: table => new
                {
                    CommentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketComments", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_TicketComments_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "TicketId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketComments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_TicketId",
                table: "TicketComments",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_AuthorId",
                table: "TicketComments",
                column: "AuthorId");

            // 9. AuditLogs Table
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    LogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.LogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditLogs");
            migrationBuilder.DropTable(name: "TicketComments");
            migrationBuilder.DropTable(name: "Tickets");
            migrationBuilder.DropTable(name: "ComplianceAudits");
            migrationBuilder.DropTable(name: "MaterialFormats");
            migrationBuilder.DropTable(name: "Materials");
            migrationBuilder.DropTable(name: "AccessibilityProfiles");

            migrationBuilder.DropForeignKey(name: "FK_Courses_AspNetUsers_TeacherId", table: "Courses");
            migrationBuilder.DropIndex(name: "IX_Courses_TeacherId", table: "Courses");

            migrationBuilder.DropColumn(name: "ActiveTerm", table: "Courses");
            migrationBuilder.DropColumn(name: "AccessibilityScore", table: "Courses");
            migrationBuilder.DropColumn(name: "TeacherId", table: "Courses");
            migrationBuilder.DropColumn(name: "Department", table: "Courses");

            migrationBuilder.DropColumn(name: "Department", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "AccessibilityNeed", table: "AspNetUsers");
        }
    }
}


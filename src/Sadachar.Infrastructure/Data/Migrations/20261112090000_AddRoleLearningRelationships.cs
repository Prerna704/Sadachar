using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Sadachar.Infrastructure.Data;

#nullable disable

namespace Sadachar.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261112090000_AddRoleLearningRelationships")]
public partial class AddRoleLearningRelationships : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("ParentStudentLinks", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            ParentUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            StudentUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            LinkedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_ParentStudentLinks", x => x.Id);
            table.ForeignKey("FK_ParentStudentLinks_AspNetUsers_ParentUserId", x => x.ParentUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.NoAction);
            table.ForeignKey("FK_ParentStudentLinks_AspNetUsers_StudentUserId", x => x.StudentUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.NoAction);
        });

        migrationBuilder.CreateTable("VolunteerStudentAssignments", table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            VolunteerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            StudentUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_VolunteerStudentAssignments", x => x.Id);
            table.ForeignKey("FK_VolunteerStudentAssignments_AspNetUsers_StudentUserId", x => x.StudentUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.NoAction);
            table.ForeignKey("FK_VolunteerStudentAssignments_AspNetUsers_VolunteerUserId", x => x.VolunteerUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.NoAction);
        });

        migrationBuilder.CreateIndex("IX_ParentStudentLinks_ParentUserId_StudentUserId", "ParentStudentLinks", new[] { "ParentUserId", "StudentUserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_ParentStudentLinks_StudentUserId", "ParentStudentLinks", "StudentUserId");
        migrationBuilder.CreateIndex("IX_VolunteerStudentAssignments_VolunteerUserId_StudentUserId", "VolunteerStudentAssignments", new[] { "VolunteerUserId", "StudentUserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_VolunteerStudentAssignments_StudentUserId", "VolunteerStudentAssignments", "StudentUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ParentStudentLinks");
        migrationBuilder.DropTable("VolunteerStudentAssignments");
    }
}

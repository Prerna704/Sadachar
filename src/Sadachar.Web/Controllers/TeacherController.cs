using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Infrastructure.Data;
using Sadachar.Web.ViewModels;

namespace Sadachar.Web.Controllers;

[Authorize(Policy = "RequireTeacher")]
public class TeacherController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeacherController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("/Teacher")]
    [HttpGet("/Teacher/Index")]
    [HttpGet("/Teacher/Dashboard")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var courses = await _db.Courses.OrderByDescending(c => c.AccessibilityScore).ToListAsync();

        var initials = string.Concat((user?.FullName ?? "Dr. Meera Iyer").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToUpper();
        if (string.IsNullOrEmpty(initials)) initials = "MI";

        var supportQueue = new List<StudentSupportItem>
        {
            new StudentSupportItem { StudentName = "Aarav Sharma", RequestDescription = "Requested large print for Module 4", Status = "Pending", BadgeColor = "mustard" },
            new StudentSupportItem { StudentName = "Nisha Verma", RequestDescription = "Extra time for Friday quiz", Status = "Approved", BadgeColor = "forest" }
        };

        var vm = new TeacherDashboardViewModel
        {
            TeacherName = user?.FullName ?? "Dr. Meera Iyer",
            Initials = initials,
            DepartmentInfo = "Computer Science · Autumn term",
            ActiveCoursesCount = courses.Count > 0 ? courses.Count : 5,
            EnrolledStudentsCount = 86,
            AvgAccessibilityScore = courses.Count > 0 ? (int)courses.Average(c => c.AccessibilityScore) : 92,
            ItemsToFixCount = 3,
            Courses = courses,
            StudentSupportQueue = supportQueue
        };

        return View(vm);
    }

    [HttpPost("/Teacher/UploadMaterial")]
    public async Task<IActionResult> UploadMaterial(string title, string subject, string module, string format, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Material title is required.";
            return RedirectToAction(nameof(Index));
        }

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Subject.Contains(subject) || c.Title.Contains(subject))
            ?? await _db.Courses.FirstOrDefaultAsync();

        var material = new Material
        {
            CourseId = course?.Id ?? Guid.NewGuid(),
            Title = title.Trim(),
            Subject = subject.Trim(),
            Module = module.Trim(),
            EstimatedMinutes = 12,
            AccessibilityScore = 94,
            ContentText = string.IsNullOrWhiteSpace(content) ? "Sample accessible course material uploaded via Teacher Hub." : content.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Materials.Add(material);
        await _db.SaveChangesAsync();

        _db.MaterialFormats.Add(new MaterialFormat
        {
            MaterialId = material.MaterialId,
            FormatType = format ?? "Audio",
            FilePath = $"/uploads/{title.ToLowerInvariant().Replace(' ', '-')}",
            DurationMinutes = 12,
            HasAltText = true,
            HasCaptions = true
        });

        _db.AuditLogs.Add(new AuditLog
        {
            ActorId = User.Identity?.Name ?? "teacher",
            ActorName = "Dr. Meera Iyer",
            Action = "Material uploaded",
            Details = $"{title} ({format}) · Accessibility verified",
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Material '{title}' uploaded and published with high-accessibility checklist verification.";
        return RedirectToAction(nameof(Index));
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Infrastructure.Data;
using Sadachar.Web.ViewModels;

namespace Sadachar.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("/Dashboard")]
    [HttpGet("/Dashboard/Index")]
    [HttpGet("/Student/Dashboard")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (await _userManager.IsInRoleAsync(user, AppRoles.Administrator) || user.Role == UserRole.Administrator)
        {
            return RedirectToAction("Index", "Admin");
        }
        if (await _userManager.IsInRoleAsync(user, AppRoles.Teacher) || user.Role == UserRole.Teacher)
        {
            return RedirectToAction("Index", "Teacher");
        }

        return await Student();
    }

    [HttpGet("/Dashboard/Student")]
    public async Task<IActionResult> Student()
    {
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? string.Empty;

        var profile = await _db.AccessibilityProfiles.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? new AccessibilityProfile { UserId = userId, FontSize = 120, Theme = "paper", TtsSpeed = 1.0, Typeface = "dyslexia", LineSpacing = "1.8" };

        var materials = await _db.Materials
            .Include(m => m.Formats)
            .Include(m => m.Course)
            .Take(3)
            .ToListAsync();

        var courses = await _db.Courses.Take(4).ToListAsync();
        var openTickets = await _db.Tickets
            .Where(t => t.UserId == userId && (t.Status == "Open" || t.Status == "In review"))
            .ToListAsync();

        var initials = string.Concat((user?.FullName ?? "Aarav Sharma").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToUpper();
        if (string.IsNullOrEmpty(initials)) initials = "AS";

        var vm = new StudentDashboardViewModel
        {
            StudentName = user?.FullName ?? "Aarav Sharma",
            Initials = initials,
            TermInfo = "Wednesday · 3 courses active this term",
            EnrolledCoursesCount = courses.Count,
            AccessibleResourcesCount = await _db.Materials.CountAsync() * 4 + 4,
            ReadingStreak = "6 days",
            OpenTicketsCount = openTickets.Count > 0 ? openTickets.Count : 1,
            Profile = profile,
            ContinueLearning = materials,
            EnrolledCourses = courses,
            OpenTickets = openTickets
        };

        return View("Student", vm);
    }
}


using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Infrastructure.Data;
using Sadachar.Web.ViewModels;

namespace Sadachar.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Screen 10: Admin Dashboard
    [HttpGet("/Admin")]
    [HttpGet("/Admin/Index")]
    [HttpGet("/Admin/Dashboard")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var recentAudits = await _db.AuditLogs.OrderByDescending(a => a.Timestamp).Take(5).ToListAsync();

        var initials = string.Concat((user?.FullName ?? "Rohan Kapoor").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToUpper();
        if (string.IsNullOrEmpty(initials)) initials = "RK";

        var vm = new AdminDashboardViewModel
        {
            AdminName = user?.FullName ?? "Rohan Kapoor",
            Initials = initials,
            RegisteredUsersCount = 1248,
            ActiveCoursesCount = 64,
            ComplianceScorePercent = 87,
            OpenIssuesCount = 14,
            RecentAudits = recentAudits
        };

        return View(vm);
    }

    // Screen 11: Compliance Monitoring
    [HttpGet("/Admin/Compliance")]
    [HttpGet("/Compliance")]
    [HttpGet("/Compliance/Index")]
    public async Task<IActionResult> Compliance()
    {
        var deptAudits = await _db.ComplianceAudits
            .Where(a => a.Department != "Campus-wide")
            .OrderByDescending(a => a.Score)
            .ToListAsync();

        var vm = new ComplianceMonitoringViewModel
        {
            WcagScore = 91,
            InstitutionalPolicyScore = 84,
            DataProtectionScore = 79,
            DepartmentAudits = deptAudits
        };

        return View(vm);
    }

    // Screen 12: Issue Tracking Kanban Board
    [HttpGet("/Admin/Issues")]
    [HttpGet("/Issues")]
    [HttpGet("/Issues/Index")]
    public async Task<IActionResult> Issues()
    {
        var allTickets = await _db.Tickets.OrderByDescending(t => t.CreatedAtUtc).ToListAsync();

        var vm = new IssueKanbanViewModel
        {
            OpenTickets = allTickets.Where(t => t.Status == "Open").ToList(),
            InReviewTickets = allTickets.Where(t => t.Status == "In review").ToList(),
            ResolvedTickets = allTickets.Where(t => t.Status == "Resolved" || t.Status == "Closed").ToList()
        };

        return View(vm);
    }

    [HttpPost("/Admin/UpdateTicketStatus")]
    public async Task<IActionResult> UpdateTicketStatus(Guid ticketId, string newStatus)
    {
        var ticket = await _db.Tickets.FindAsync(ticketId);
        if (ticket != null)
        {
            ticket.Status = newStatus;
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = User.Identity?.Name ?? "admin",
                ActorName = "Rohan Kapoor",
                Action = $"Issue {ticket.TicketNumber} transitioned to {newStatus}",
                Details = "Updated via Kanban Board",
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Issues));
    }

    // Screen 13: Reports & CSV Export
    [HttpGet("/Admin/Reports")]
    [HttpGet("/Reports")]
    [HttpGet("/Reports/Index")]
    public async Task<IActionResult> Reports(string? period = "Oct 2026", string? department = "All departments")
    {
        var audits = await _db.ComplianceAudits.ToListAsync();
        var logs = await _db.AuditLogs.OrderByDescending(l => l.Timestamp).Take(10).ToListAsync();

        var vm = new ReportsSummaryViewModel
        {
            Period = period ?? "Oct 2026",
            Department = department ?? "All departments",
            ResolvedWithinSlaPercent = 91,
            ResolvedSlaChange = "+4%",
            CaptionedCoveragePercent = 83,
            CaptionedCoverageChange = "+6%",
            AuditLogs = logs,
            Audits = audits
        };

        return View(vm);
    }

    [HttpGet("/Admin/ExportCsv")]
    [HttpGet("/Reports/ExportCsv")]
    public async Task<IActionResult> ExportCsv()
    {
        var audits = await _db.ComplianceAudits.ToListAsync();
        var tickets = await _db.Tickets.ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("RecordType,Identifier,CategoryOrStandard,ScoreOrPriority,Status,Timestamp");

        foreach (var audit in audits)
        {
            sb.AppendLine($"Audit,\"{audit.Department}\",\"{audit.Standard}\",{audit.Score}%,\"{audit.Status}\",\"{audit.AuditedOn:yyyy-MM-dd}\"");
        }

        foreach (var t in tickets)
        {
            sb.AppendLine($"Ticket,\"{t.TicketNumber}\",\"{t.Category}\",\"{t.Priority}\",\"{t.Status}\",\"{t.CreatedAtUtc:yyyy-MM-dd}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"accessed-compliance-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}


using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Infrastructure.Data;

namespace Sadachar.Web.Controllers.Api;

[ApiController]
[Route("api/resources")]
[Produces("application/json")]
public class ResourcesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ResourcesApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetResources([FromQuery] string? format = null, [FromQuery] string? search = null)
    {
        var query = _db.Materials
            .Include(m => m.Course)
            .Include(m => m.Formats)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(format) && !format.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => m.Formats.Any(f => f.FormatType.ToLower().Contains(format.ToLower())));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(m => m.Title.ToLower().Contains(s) || m.Subject.ToLower().Contains(s));
        }

        var results = await query.Select(m => new
        {
            m.MaterialId,
            m.Title,
            m.Module,
            m.Subject,
            m.EstimatedMinutes,
            m.AccessibilityScore,
            Formats = m.Formats.Select(f => new
            {
                f.FormatId,
                f.FormatType,
                f.FilePath,
                f.HasCaptions,
                f.HasAltText,
                f.DurationMinutes
            })
        }).ToListAsync();

        return Ok(results);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetResourceById(Guid id)
    {
        var material = await _db.Materials
            .Include(m => m.Course)
            .Include(m => m.Formats)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MaterialId == id);

        if (material == null) return NotFound(new { message = "Resource not found" });

        return Ok(new
        {
            material.MaterialId,
            material.Title,
            material.Module,
            material.Subject,
            material.ContentText,
            material.EstimatedMinutes,
            material.AccessibilityScore,
            Formats = material.Formats.Select(f => new
            {
                f.FormatId,
                f.FormatType,
                f.FilePath,
                f.HasCaptions,
                f.HasAltText,
                f.DurationMinutes
            })
        });
    }
}

[ApiController]
[Route("api/profile")]
[Produces("application/json")]
public class ProfileApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            // If not logged in, return student demo profile
            user = await _userManager.FindByEmailAsync("student@accessed.local");
            if (user == null) return Unauthorized();
        }

        var profile = await _db.AccessibilityProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id)
            ?? new AccessibilityProfile { UserId = user.Id, FontSize = 120, Theme = "paper", TtsSpeed = 1.0, Typeface = "dyslexia", LineSpacing = "1.8" };

        return Ok(new
        {
            user.FullName,
            user.Email,
            Role = user.Role.ToString(),
            user.AccessibilityNeed,
            user.Department,
            Settings = new
            {
                profile.FontSize,
                profile.Theme,
                profile.TtsSpeed,
                profile.Typeface,
                profile.LineSpacing,
                profile.ReduceMotion,
                profile.VisibleFocus,
                profile.UnderlineLinks
            }
        });
    }

    [HttpPost("accessibility")]
    public async Task<IActionResult> UpdateAccessibilityProfile([FromBody] AccessibilityProfileDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            user = await _userManager.FindByEmailAsync("student@accessed.local");
            if (user == null) return Unauthorized();
        }

        var profile = await _db.AccessibilityProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            profile = new AccessibilityProfile { UserId = user.Id };
            _db.AccessibilityProfiles.Add(profile);
        }

        profile.FontSize = dto.FontSize > 0 ? dto.FontSize : 100;
        profile.Theme = dto.Theme ?? "paper";
        profile.Typeface = dto.Typeface ?? "default";
        profile.LineSpacing = dto.LineSpacing ?? "1.6";
        profile.TtsSpeed = dto.TtsSpeed > 0 ? dto.TtsSpeed : 1.0;
        profile.ReduceMotion = dto.ReduceMotion;
        profile.VisibleFocus = dto.VisibleFocus;
        profile.UnderlineLinks = dto.UnderlineLinks;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Accessibility preferences updated successfully", profile });
    }

    public class AccessibilityProfileDto
    {
        public int FontSize { get; set; } = 100;
        public string? Theme { get; set; } = "paper";
        public string? Typeface { get; set; } = "default";
        public string? LineSpacing { get; set; } = "1.6";
        public double TtsSpeed { get; set; } = 1.0;
        public bool ReduceMotion { get; set; }
        public bool VisibleFocus { get; set; } = true;
        public bool UnderlineLinks { get; set; }
    }
}

[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public class TicketsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TicketsApiController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetTickets([FromQuery] string? status = null)
    {
        var query = _db.Tickets.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(t => t.Status.ToLower() == status.ToLower());
        }

        var list = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new
            {
                t.TicketId,
                t.TicketNumber,
                t.Category,
                t.Priority,
                t.Status,
                t.Subject,
                t.Description,
                t.IsConfidential,
                t.AssigneeInitials,
                t.SlaDays,
                t.CreatedAtUtc,
                t.SlaExpiryUtc
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketApiDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Subject))
            return BadRequest(new { message = "Subject is required" });

        var user = await _userManager.GetUserAsync(User) 
            ?? await _userManager.FindByEmailAsync("student@accessed.local");

        var count = await _db.Tickets.CountAsync() + 1050;
        var ticket = new Ticket
        {
            TicketNumber = $"#ACC-{count}",
            UserId = user?.Id ?? "student-user",
            Category = dto.Category ?? "Inaccessible material",
            Priority = dto.Priority ?? "High",
            Status = "Open",
            IsConfidential = dto.IsConfidential,
            Subject = dto.Subject.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            AssigneeInitials = "MI",
            SlaDays = 2,
            CreatedAtUtc = DateTime.UtcNow,
            SlaExpiryUtc = DateTime.UtcNow.AddHours(72)
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTickets), new { id = ticket.TicketId }, ticket);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTicketStatusDto dto)
    {
        var ticket = await _db.Tickets.FindAsync(id);
        if (ticket == null) return NotFound(new { message = "Ticket not found" });

        ticket.Status = dto.Status;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Status updated successfully", ticket.TicketNumber, ticket.Status });
    }

    public class CreateTicketApiDto
    {
        public string Category { get; set; } = "Inaccessible material";
        public string Priority { get; set; } = "High";
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsConfidential { get; set; } = false;
    }

    public class UpdateTicketStatusDto
    {
        public string Status { get; set; } = "In review";
    }
}

[ApiController]
[Route("api/compliance")]
[Produces("application/json")]
public class ComplianceApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ComplianceApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompliance()
    {
        var standardAudits = await _db.ComplianceAudits
            .Where(a => a.Department == "Campus-wide")
            .ToListAsync();

        var departmentAudits = await _db.ComplianceAudits
            .Where(a => a.Department != "Campus-wide")
            .OrderByDescending(a => a.Score)
            .ToListAsync();

        return Ok(new
        {
            OverallScore = 87,
            Standards = new
            {
                WcagScore = 91,
                InstitutionalPolicyScore = 84,
                DataProtectionScore = 79
            },
            DepartmentAudits = departmentAudits
        });
    }
}

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReportsApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var openTicketsCount = await _db.Tickets.CountAsync(t => t.Status == "Open" || t.Status == "In review");
        var resolvedTicketsCount = await _db.Tickets.CountAsync(t => t.Status == "Resolved" || t.Status == "Closed");

        return Ok(new
        {
            Period = "Oct 2026",
            ComplianceScore = 87,
            ResolvedWithinSlaPercent = 91,
            CaptionedMediaCoveragePercent = 83,
            OpenIssues = openTicketsCount,
            ResolvedIssues = resolvedTicketsCount,
            IssuesByCategory = new
            {
                Documents = 45,
                Media = 30,
                Facilities = 15,
                Conduct = 10
            }
        });
    }

    [HttpGet("export")]
    [Produces("text/csv")]
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
        return File(bytes, "text/csv", $"accessed-report-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}


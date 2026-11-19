using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Infrastructure.Data;
using Sadachar.Web.ViewModels;

namespace Sadachar.Web.Controllers;

[Authorize]
public class SupportController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SupportController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("/Support")]
    [HttpGet("/Support/Index")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? string.Empty;

        var myTickets = await _db.Tickets
            .Where(t => t.UserId == userId || (t.User != null && t.User.Email == "student@accessed.local"))
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();

        var vm = new SupportPortalViewModel
        {
            MyTickets = myTickets,
            NewTicket = new CreateTicketInput()
        };

        return View(vm);
    }

    [HttpPost("/Support/Create")]
    public async Task<IActionResult> Create(CreateTicketInput input)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            var myTickets = await _db.Tickets
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();

            var vm = new SupportPortalViewModel
            {
                MyTickets = myTickets,
                NewTicket = input
            };
            return View("Index", vm);
        }

        var count = await _db.Tickets.CountAsync() + 1050;
        var ticketNumber = $"#ACC-{count}";

        var teacher = await _userManager.FindByEmailAsync("teacher@accessed.local");

        var ticket = new Ticket
        {
            TicketNumber = ticketNumber,
            UserId = user.Id,
            Category = input.Category,
            Priority = input.Priority,
            Status = "Open",
            IsConfidential = input.IsConfidential,
            Subject = input.Subject.Trim(),
            Description = input.Description.Trim(),
            AssigneeId = teacher?.Id,
            AssigneeInitials = "MI",
            SlaDays = 2,
            CreatedAtUtc = DateTime.UtcNow,
            SlaExpiryUtc = DateTime.UtcNow.AddHours(72)
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Support request {ticketNumber} has been logged. Our team will review within 2 days.";
        return RedirectToAction(nameof(Index));
    }
}

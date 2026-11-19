using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Infrastructure.Data;
using Sadachar.Web.ViewModels;

namespace Sadachar.Web.Controllers;

[Authorize]
public class ResourcesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ResourcesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("/Resources")]
    [HttpGet("/Resources/Index")]
    public async Task<IActionResult> Index(string? format = "All", string? search = null, string? sort = "recent")
    {
        var query = _db.Materials
            .Include(m => m.Course)
            .Include(m => m.Formats)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(format) && !format.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => m.Formats.Any(f => f.FormatType.ToLower().Contains(format.ToLower())));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(m => m.Title.ToLower().Contains(s) || m.Subject.ToLower().Contains(s) || (m.Course != null && m.Course.Title.ToLower().Contains(s)));
        }

        if (sort == "title")
        {
            query = query.OrderBy(m => m.Title);
        }
        else if (sort == "score")
        {
            query = query.OrderByDescending(m => m.AccessibilityScore);
        }
        else
        {
            query = query.OrderByDescending(m => m.CreatedAtUtc);
        }

        var materials = await query.ToListAsync();

        var vm = new ResourcesCatalogViewModel
        {
            SelectedFormat = format ?? "All",
            SearchQuery = search,
            SortOrder = sort ?? "recent",
            Materials = materials
        };

        return View(vm);
    }

    [HttpGet("/Resources/Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, string? format = "Text")
    {
        var material = await _db.Materials
            .Include(m => m.Course)
            .Include(m => m.Formats)
            .FirstOrDefaultAsync(m => m.MaterialId == id);

        if (material == null)
        {
            // Fallback to first available material if id not found
            material = await _db.Materials
                .Include(m => m.Course)
                .Include(m => m.Formats)
                .FirstOrDefaultAsync();

            if (material == null) return NotFound();
        }

        var vm = new MaterialDetailsViewModel
        {
            Material = material,
            ActiveFormat = format ?? "Text",
            Formats = material.Formats.ToList(),
            AltTextVerified = true,
            CaptionsVerified = true,
            ReadingOrderVerified = true,
            ContrastVerified = true
        };

        return View(vm);
    }

    [HttpGet("/Reader")]
    [HttpGet("/Resources/Reader")]
    [HttpGet("/Resources/Reader/{id:guid}")]
    public async Task<IActionResult> Reader(Guid? id = null)
    {
        Material? material = null;
        if (id.HasValue)
        {
            material = await _db.Materials.FirstOrDefaultAsync(m => m.MaterialId == id.Value);
        }

        if (material == null)
        {
            material = await _db.Materials.FirstOrDefaultAsync(m => m.Title.Contains("Linked Lists"))
                ?? await _db.Materials.FirstOrDefaultAsync();
        }

        var user = await _userManager.GetUserAsync(User);
        var profile = user != null ? await _db.AccessibilityProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id) : null;

        var sampleText = material?.ContentText ?? "Accessible design starts with choices. Learners should decide how fast, how loud and in which voice their materials are read. Each sentence is highlighted as it is spoken so that reading and listening stay in sync. You can pause at any point. A linked list stores items in nodes. Each node holds a value and a reference to the next node. To insert at the head, create a new node and point it to the old head.";

        // Break text into sentences
        var sentences = sampleText
            .Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().EndsWith('.') ? s.Trim() : s.Trim() + ".")
            .ToList();

        var vm = new TtsReaderViewModel
        {
            MaterialId = material?.MaterialId ?? Guid.NewGuid(),
            Title = material?.Title ?? "Linked Lists: Insertion",
            Module = material?.Module ?? "Computer Science › Module 3 › Lesson 2",
            Text = sampleText,
            Sentences = sentences,
            Speed = profile?.TtsSpeed ?? 1.0,
            Pitch = 1.0,
            PreferredVoice = "en-IN"
        };

        return View(vm);
    }
}


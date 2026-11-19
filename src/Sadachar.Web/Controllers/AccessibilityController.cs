using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Infrastructure.Data;
using Sadachar.Web.ViewModels;

namespace Sadachar.Web.Controllers;

[Authorize]
public class AccessibilityController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccessibilityController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("/Accessibility")]
    [HttpGet("/Accessibility/Index")]
    [HttpGet("/Settings/Accessibility")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? string.Empty;

        var profile = await _db.AccessibilityProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new AccessibilityProfile
            {
                UserId = userId,
                FontSize = 120,
                Theme = "paper",
                TtsSpeed = 1.0,
                Typeface = "default",
                LineSpacing = "1.8",
                ReduceMotion = false,
                VisibleFocus = true,
                UnderlineLinks = false
            };
        }

        var vm = new AccessibilitySettingsViewModel
        {
            FontSize = profile.FontSize,
            Theme = profile.Theme,
            Typeface = profile.Typeface,
            LineSpacing = profile.LineSpacing,
            ReduceMotion = profile.ReduceMotion,
            VisibleFocus = profile.VisibleFocus,
            UnderlineLinks = profile.UnderlineLinks,
            TtsSpeed = profile.TtsSpeed
        };

        return View(vm);
    }

    [HttpPost("/Accessibility/Save")]
    public async Task<IActionResult> Save(AccessibilitySettingsViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var profile = await _db.AccessibilityProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            profile = new AccessibilityProfile { UserId = user.Id };
            _db.AccessibilityProfiles.Add(profile);
        }

        profile.FontSize = model.FontSize;
        profile.Theme = model.Theme ?? "paper";
        profile.Typeface = model.Typeface ?? "default";
        profile.LineSpacing = model.LineSpacing ?? "1.8";
        profile.ReduceMotion = model.ReduceMotion;
        profile.VisibleFocus = model.VisibleFocus;
        profile.UnderlineLinks = model.UnderlineLinks;
        profile.TtsSpeed = model.TtsSpeed;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Also set persistent cookies so layout renders seamlessly before JavaScript even boots
        Response.Cookies.Append("accessed_theme", profile.Theme, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" });
        Response.Cookies.Append("accessed_font_size", profile.FontSize.ToString(), new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" });
        Response.Cookies.Append("accessed_typeface", profile.Typeface, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" });
        Response.Cookies.Append("accessed_spacing", profile.LineSpacing, new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), Path = "/" });

        TempData["SuccessMessage"] = "Accessibility settings saved and applied site-wide.";
        return RedirectToAction(nameof(Index));
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Sadachar.Core.Entities;
using Sadachar.Core.Enums;
using Sadachar.Infrastructure.Data;
using Sadachar.Web.ViewModels;

namespace Sadachar.Web.Controllers;

[AutoValidateAntiforgeryToken]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly IAntiforgery _antiforgery;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        IAntiforgery antiforgery)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _antiforgery = antiforgery;
    }

    [AllowAnonymous]
    [HttpGet("/api/account/csrf")]
    public IActionResult CsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { RequestToken = tokens.RequestToken });
    }

    [Authorize]
    [HttpGet("/api/account/me")]
    public async Task<IActionResult> CurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null || !user.IsActive) return Unauthorized();

        return Ok(new
        {
            user.FullName,
            user.Email,
            Role = user.Role.ToString(),
            Disability = user.Disability.ToString(),
            user.AccessibilityNeed,
            user.Department
        });
    }

    [AllowAnonymous]
    [HttpGet("/Account/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToUserDashboard();
        }

        ViewBag.ActiveTab = "login";
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost("/Account/Login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ActiveTab = "login";
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
            ViewBag.ActiveTab = "login";
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            if (user.Role == UserRole.Administrator || await _userManager.IsInRoleAsync(user, AppRoles.Administrator))
            {
                return RedirectToAction("Index", "Admin");
            }
            if (user.Role == UserRole.Teacher || await _userManager.IsInRoleAsync(user, AppRoles.Teacher))
            {
                return RedirectToAction("Index", "Teacher");
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts. Please try again in 5 minutes.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
        }

        ViewBag.ActiveTab = "login";
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet("/Account/Register")]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToUserDashboard();
        }

        ViewBag.ActiveTab = "register";
        return View("Login", new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost("/Account/Register")]
    public async Task<IActionResult> Register(StudentRegistrationViewModel model)
    {
        // Guard against unauthorized admin registration
        if ((int)model.Role == (int)UserRole.Administrator)
        {
            ModelState.AddModelError(string.Empty, "Administrator accounts cannot be registered publicly. Contact your institution IT.");
            ViewBag.ActiveTab = "register";
            return View("Login", new LoginViewModel());
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ActiveTab = "register";
            ViewBag.RegisterModel = model;
            return View("Login", new LoginViewModel());
        }

        var assignedRole = model.Role == RegistrationRole.Teacher ? UserRole.Teacher : UserRole.Student;
        var roleName = model.Role == RegistrationRole.Teacher ? AppRoles.Teacher : AppRoles.Student;

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            Role = assignedRole,
            AccessibilityNeed = model.AccessibilityNeeds != null && model.AccessibilityNeeds.Count > 0
                ? string.Join(", ", model.AccessibilityNeeds)
                : "None",
            Department = model.Department ?? (assignedRole == UserRole.Teacher ? "Computer Science" : "BCA Semester 5"),
            Disability = DisabilityType.None,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            ViewBag.ActiveTab = "register";
            ViewBag.RegisterModel = model;
            return View("Login", new LoginViewModel());
        }

        await _userManager.AddToRoleAsync(user, roleName);

        // Create default AccessibilityProfile
        _db.AccessibilityProfiles.Add(new AccessibilityProfile
        {
            UserId = user.Id,
            FontSize = 100,
            Theme = "paper",
            TtsSpeed = 1.0,
            Typeface = "default",
            LineSpacing = "1.6",
            ReduceMotion = false,
            VisibleFocus = true,
            UnderlineLinks = false
        });
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToUserDashboard(user);
    }

    [Authorize]
    [HttpGet("/Profile")]
    [HttpGet("/Account/Profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        var initials = string.Concat(user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToUpper();
        if (string.IsNullOrEmpty(initials)) initials = "U";

        var vm = new UserProfileEditViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString(),
            Department = user.Department ?? "BCA Semester 5",
            AccessibilityNeed = user.AccessibilityNeed ?? "Low vision",
            Initials = initials,
            EmailUpdatesEnabled = true,
            ShareNeedsWithTeachers = true,
            TwoFactorEnabled = false
        };

        return View(vm);
    }

    [Authorize]
    [HttpPost("/Account/Profile")]
    public async Task<IActionResult> Profile(UserProfileEditViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.AccessibilityNeed = model.AccessibilityNeed;
        user.Department = model.Department;
        await _userManager.UpdateAsync(user);

        TempData["SuccessMessage"] = "Profile details updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost("/Account/Logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToUserDashboard(ApplicationUser? user = null)
    {
        if (User.IsInRole(AppRoles.Administrator) || user?.Role == UserRole.Administrator)
            return RedirectToAction("Index", "Admin");
        if (User.IsInRole(AppRoles.Teacher) || user?.Role == UserRole.Teacher)
            return RedirectToAction("Index", "Teacher");
        return RedirectToAction("Index", "Dashboard");
    }
}

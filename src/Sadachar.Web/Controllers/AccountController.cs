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

[Route("[controller]/[action]")]
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
        if (user is null || !user.IsActive)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            user.FullName,
            user.Email,
            Role = user.Role.ToString(),
            Disability = user.Disability.ToString()
        });
    }

    [Authorize]
    [HttpPut("/api/account/me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] ProfileUpdateRequest request)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null || !user.IsActive) return Unauthorized();
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 150) return BadRequest(new { message = "Enter a name up to 150 characters." });
        user.FullName = request.FullName.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));
        await _signInManager.RefreshSignInAsync(user);
        return Ok(new { user.FullName, user.Email });
    }

    public record ProfileUpdateRequest(string FullName);

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Profile));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive || user.Role != model.Role)
        {
            ModelState.AddModelError(string.Empty, "Email, password, or account type is incorrect.");
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

            return RedirectToAction(nameof(Profile));
        }

        ModelState.AddModelError(string.Empty, "Email, password, or account type is incorrect.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Profile));
        }

        return View(new StudentRegistrationViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register(StudentRegistrationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            Role = UserRole.Student,
            Disability = DisabilityType.None,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    error.Code == "DuplicateEmail" || error.Code == "DuplicateUserName"
                        ? nameof(model.Email)
                        : nameof(model.Password),
                    error.Description);
            }

            return View(model);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        return View(user);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}

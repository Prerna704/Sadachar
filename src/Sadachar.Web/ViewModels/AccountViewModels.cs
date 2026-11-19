using System.ComponentModel.DataAnnotations;
using Sadachar.Core.Enums;

namespace Sadachar.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class StudentRegistrationViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Register as")]
    public RegistrationRole Role { get; set; } = RegistrationRole.Student;

    [Display(Name = "Accessibility needs (optional)")]
    public List<string> AccessibilityNeeds { get; set; } = [];

    [Display(Name = "Department / Course")]
    public string? Department { get; set; }
}

public enum RegistrationRole
{
    Student = 1,
    Teacher = 2,
    Parent = 5,
    Volunteer = 6
}

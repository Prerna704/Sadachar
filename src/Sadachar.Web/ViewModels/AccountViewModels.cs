using System.ComponentModel.DataAnnotations;
using Sadachar.Core.Enums;

namespace Sadachar.Web.ViewModels;

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class StudentRegistrationViewModel
{
    [Required]
    [StringLength(150)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [EnumDataType(typeof(RegistrationRole))]
    [Display(Name = "I’m joining as")]
    public RegistrationRole Role { get; set; } = RegistrationRole.Student;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please accept the terms to create an account.")]
    [Display(Name = "I agree to the Terms and Conditions")]
    public bool AcceptTerms { get; set; }
}

public enum RegistrationRole
{
    Student = 1,
    Parent = 5,
    Volunteer = 6
}

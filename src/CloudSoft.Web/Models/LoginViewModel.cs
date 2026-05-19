using System.ComponentModel.DataAnnotations;

namespace CloudSoft.Web.Models;

/// <summary>View model for the login form.</summary>
public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

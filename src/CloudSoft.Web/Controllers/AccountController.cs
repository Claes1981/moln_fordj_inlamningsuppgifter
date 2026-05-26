using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CloudSoft.Data;
using CloudSoft.Web.Models;

namespace CloudSoft.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User '{Username}' logged in successfully.", model.Username);
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Login attempt for locked-out user '{Username}'.", model.Username);
            ModelState.AddModelError("", "Account locked out.");
        }
        else
        {
            _logger.LogWarning("Failed login attempt for user '{Username}'.", model.Username);
            ModelState.AddModelError("", "Invalid username or password.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name ?? "Unknown";
        _logger.LogInformation("User '{Username}' logged out.", username);
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

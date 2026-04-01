using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Users;
using QueryNest.Web.Models.Profile;

namespace QueryNest.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IProfileService _profileService;
    private readonly IWebHostEnvironment _environment;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public ProfileController(IProfileService profileService, IWebHostEnvironment environment, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _profileService = profileService;
        _environment = environment;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var profile = await _profileService.GetProfileAsync(userId, cancellationToken);
        if (profile is null)
        {
            return RedirectToAction("Login", "Account");
        }

        return View(new ProfileViewModel
        {
            Email = profile.Email,
            Name = profile.Name,
            Bio = profile.Bio,
            AvatarPath = profile.AvatarPath,
            Reputation = profile.Reputation
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            var current = await _profileService.GetProfileAsync(userId, cancellationToken);
            if (current is not null)
            {
                model.Email = current.Email;
                model.AvatarPath = current.AvatarPath;
                model.Reputation = current.Reputation;
            }

            return View("Index", model);
        }

        var result = await _profileService.UpdateProfileAsync(
            userId,
            new UpdateProfileRequestDto
            {
                Name = model.Name,
                Bio = model.Bio
            },
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View("Index", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAvatar(IFormFile avatar, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (avatar is null || avatar.Length == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
        var allowed = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        if (!allowed.Contains(extension))
        {
            return RedirectToAction(nameof(Index));
        }

        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await avatar.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"/uploads/avatars/{fileName}";
        await _profileService.UpdateAvatarAsync(userId, relativePath, cancellationToken);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ChangePassword(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var hasPassword = await _userManager.HasPasswordAsync(user);
        return View(new ChangePasswordViewModel { HasPassword = hasPassword });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return RedirectToAction("Login", "Account");
        }

        model.HasPassword = await _userManager.HasPasswordAsync(user);
        if (model.HasPassword && string.IsNullOrWhiteSpace(model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(ChangePasswordViewModel.CurrentPassword), "Current password is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        IdentityResult result;
        if (model.HasPassword)
        {
            result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword!, model.NewPassword);
        }
        else
        {
            result = await _userManager.AddPasswordAsync(user, model.NewPassword);
        }

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Password updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}

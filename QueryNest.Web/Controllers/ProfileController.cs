using System.Security.Claims;
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

    public ProfileController(IProfileService profileService, IWebHostEnvironment environment)
    {
        _profileService = profileService;
        _environment = environment;
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
}

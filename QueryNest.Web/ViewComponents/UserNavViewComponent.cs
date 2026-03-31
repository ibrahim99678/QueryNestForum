using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueryNest.DAL.Interfaces;

namespace QueryNest.Web.ViewComponents;

public class UserNavViewComponent : ViewComponent
{
    private readonly IUnitOfWork _unitOfWork;

    public UserNavViewComponent(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new UserNavModel());
        }

        var principal = User as ClaimsPrincipal;
        var aspNetUserId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(aspNetUserId))
        {
            return View(new UserNavModel());
        }

        var user = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .Where(u => u.AspNetUserId == aspNetUserId)
            .Select(u => new { u.Name, u.AvatarPath })
            .FirstOrDefaultAsync(cancellationToken);

        var avatarPath = user?.AvatarPath;
        var avatarUrl = string.IsNullOrWhiteSpace(avatarPath)
            ? Url.Content("~/images/default-avatar.svg")
            : (avatarPath.StartsWith("/") ? avatarPath : "/" + avatarPath);

        return View(new UserNavModel
        {
            IsAuthenticated = true,
            Name = user?.Name ?? "Account",
            AvatarUrl = avatarUrl,
            IsAdminOrModerator = User.IsInRole("Admin") || User.IsInRole("Moderator")
        });
    }

    public class UserNavModel
    {
        public bool IsAuthenticated { get; init; }
        public string Name { get; init; } = "Account";
        public string AvatarUrl { get; init; } = "/images/default-avatar.svg";
        public bool IsAdminOrModerator { get; init; }
    }
}

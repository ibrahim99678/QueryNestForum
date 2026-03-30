using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Auth;
using QueryNest.Contract.Users;
using QueryNest.DAL.Interfaces;

namespace QueryNest.BLL.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public ProfileService(UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileDto?> GetProfileAsync(string aspNetUserId, CancellationToken cancellationToken = default)
    {
        var identityUser = await _userManager.FindByIdAsync(aspNetUserId);
        if (identityUser is null)
        {
            return null;
        }

        var profile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        return new UserProfileDto
        {
            UserId = profile.UserId,
            AspNetUserId = profile.AspNetUserId,
            Email = identityUser.Email ?? string.Empty,
            Name = profile.Name,
            AvatarPath = profile.AvatarPath,
            Bio = profile.Bio,
            Reputation = profile.Reputation
        };
    }

    public async Task<AuthResultDto> UpdateProfileAsync(string aspNetUserId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return AuthResultDto.Failed("Profile not found.");
        }

        profile.Name = request.Name;
        profile.Bio = request.Bio;
        profile.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> UpdateAvatarAsync(string aspNetUserId, string avatarPath, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return AuthResultDto.Failed("Profile not found.");
        }

        profile.AvatarPath = avatarPath;
        profile.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Users.Update(profile);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }
}

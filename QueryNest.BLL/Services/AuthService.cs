using Microsoft.AspNetCore.Identity;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Auth;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;

namespace QueryNest.BLL.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
        {
            return AuthResultDto.Failed("Email is already registered.");
        }

        var identityUser = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var createResult = await _userManager.CreateAsync(identityUser, request.Password);
        if (!createResult.Succeeded)
        {
            return AuthResultDto.Failed(createResult.Errors.Select(e => e.Description).ToArray());
        }

        var roleResult = await _userManager.AddToRoleAsync(identityUser, "User");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(identityUser);
            return AuthResultDto.Failed(roleResult.Errors.Select(e => e.Description).ToArray());
        }

        await _unitOfWork.Users.AddAsync(
            new UserProfile
            {
                AspNetUserId = identityUser.Id,
                Name = request.Name,
                Reputation = 0,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _signInManager.SignInAsync(identityUser, isPersistent: false);

        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return AuthResultDto.Failed("Invalid login attempt.");
        }

        var result = await _signInManager.PasswordSignInAsync(user, request.Password, request.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return AuthResultDto.Failed("Invalid login attempt.");
        }

        return AuthResultDto.Success();
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        return _signInManager.SignOutAsync();
    }
}

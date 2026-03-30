using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;

namespace QueryNest.BLL.Services;

public class InitialSeedService : IInitialSeedService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;

    public InitialSeedService(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
    }

    public async Task SeedAdminAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var adminRole = "Admin";
        if (!await _roleManager.RoleExistsAsync(adminRole))
        {
            await _roleManager.CreateAsync(new IdentityRole(adminRole));
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                return;
            }
        }

        if (!await _userManager.IsInRoleAsync(user, adminRole))
        {
            await _userManager.AddToRoleAsync(user, adminRole);
        }

        var profileExists = await _unitOfWork.Users.Query()
            .AnyAsync(x => x.AspNetUserId == user.Id, cancellationToken);

        if (!profileExists)
        {
            await _unitOfWork.Users.AddAsync(
                new UserProfile
                {
                    AspNetUserId = user.Id,
                    Name = "Admin",
                    Reputation = 0,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var hasCategory = await _unitOfWork.Categories.Query().AnyAsync(cancellationToken);
        if (!hasCategory)
        {
            await _unitOfWork.Categories.AddAsync(
                new Category
                {
                    Name = "General",
                    Slug = "general",
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

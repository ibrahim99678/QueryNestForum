using Microsoft.AspNetCore.Identity;

namespace QueryNest.Web.Security;

public static class RoleSeeder
{
    public static async Task Seed(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        var roles = new[] { "Admin", "Moderator", "User" };
        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

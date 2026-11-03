using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LeaveDesk.Infrastructure.Identity;

public static class IdentitySeeder
{
    private static readonly string[] Roles = ["Employee", "Manager", "HRAdmin"];

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        const string hrAdminEmail = "techgrit.hradmin@yopmail.com";
        if (await userManager.FindByEmailAsync(hrAdminEmail) is null)
        {
            var hrAdmin = new ApplicationUser
            {
                UserName = hrAdminEmail,
                Email = hrAdminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(hrAdmin, "Techgrit@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(hrAdmin, "HRAdmin");
            }
        }
    }
}

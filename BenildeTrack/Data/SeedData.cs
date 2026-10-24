using BenildeTrack.Models;
using Microsoft.AspNetCore.Identity;

namespace BenildeTrack.Data;

public static class SeedData
{
    // Temporary TEST account so you can log in the first time. Change or remove before any real use.
    private const string AdminEmail = "admin@benildetrack.test";
    private const string AdminPassword = "Admin12345";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        // Creates the SQLite file and tables if they don't exist yet.
        await db.Database.EnsureCreatedAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(AdminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FullName = "Test Administrator",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AppRoles.Administrator);
        }
    }
}
using System.Text.Json;
using BenildeTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Data;

public static class SeedData
{
    // Temporary TEST account so you can log in the first time. Change or remove before any real use.
    private const string AdminEmail = "admin@benildetrack.test";
    private const string AdminPassword = "Admin12345";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
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

        // Load the handbook offense list once.
        if (!await db.HandbookOffenses.AnyAsync())
        {
            var env = services.GetRequiredService<IWebHostEnvironment>();
            var path = Path.Combine(env.ContentRootPath, "Data", "handbook_offenses.json");
            if (File.Exists(path))
            {
                var items = JsonSerializer.Deserialize<List<HandbookOffense>>(
                    File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (items != null)
                {
                    db.HandbookOffenses.AddRange(items);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
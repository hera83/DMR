using DMR.Services.Identity.Dtos;
using Microsoft.AspNetCore.Identity;

namespace DMR.Data.Seeds;

/// <summary>
/// Pre-fills the fixed set of Identity roles this app needs (<see cref="IdentityRoleNames.Admin"/>,
/// <see cref="IdentityRoleNames.ApiKeyHolder"/>) — the one piece of seed data this database has today. Run
/// at startup via <c>IdentityService.EnsureSeededAsync</c>, which owns *when* seeding happens; this class
/// only owns *what* gets seeded. See data/README.md.
/// </summary>
public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { IdentityRoleNames.Admin, IdentityRoleNames.ApiKeyHolder })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Failed to seed Identity role '{role}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}

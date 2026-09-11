using Microsoft.AspNetCore.Identity;

namespace DMR.Data.Models;

/// <summary>
/// The Identity user record that owns zero-or-more <see cref="ApiKey"/> rows. There is no password/login
/// flow for these users — an <see cref="ApplicationUser"/> exists purely as the Identity-framework owner
/// of one or more API keys, created automatically by <c>IdentityService.CreateApiKeyAsync</c> when a new
/// key is issued. See services/identity/docs for why the design still goes through full ASP.NET Core
/// Identity (UserManager/RoleManager) rather than a bespoke API-key table, and data/README.md for why the
/// model itself lives here rather than under services/identity.
/// </summary>
public class ApplicationUser : IdentityUser
{
}

using DMR.Services.Identity.Dtos;
using DMR.Services.Logs.Dtos;
using DMR.Services.Logs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMR.Controllers;

/// <summary>
/// Searches the app's structured logs (see services/logs/docs) — every log event across the whole API,
/// not just this controller's own. Restricted to the <c>Admin</c> role, which only the configured master
/// key authenticates as (see <see cref="DMR.Services.Identity.Dtos.ApiKeyAuthOptions"/> and
/// services/identity/docs) — no regular API key, however privileged, can read the logs.
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
[Authorize(Roles = IdentityRoleNames.Admin)]
public class LogController(ILogsService logsService) : ControllerBase
{
    /// <summary>Searches logged events, newest first. Every filter is optional and they combine with AND:
    /// <c>levels</c> (repeat the query param per level, e.g. <c>?levels=Warning&amp;levels=Error</c>,
    /// matched case-insensitively), <c>fromUtc</c>/<c>toUtc</c> (inclusive UTC range), <c>searchText</c>
    /// (case-insensitive substring match against the message and exception text), <c>hasException</c>
    /// (true/false). <c>page</c> defaults to 1, <c>pageSize</c> defaults to 50 and is capped at 500.</summary>
    [HttpGet]
    public async Task<ActionResult<SearchLogsResponseDto>> Search([FromQuery] SearchLogsRequestDto request, CancellationToken cancellationToken)
    {
        var response = await logsService.SearchLogsAsync(request, cancellationToken);
        return Ok(response);
    }
}

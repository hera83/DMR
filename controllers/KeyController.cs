using DMR.Services.Identity.Dtos;
using DMR.Services.Identity.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DMR.Controllers;

/// <summary>
/// Administers API keys. Restricted to the <c>Admin</c> role, which only the configured master key
/// authenticates as (see <see cref="DMR.Services.Identity.Dtos.ApiKeyAuthOptions"/> and
/// services/identity/docs) — no regular API key, however privileged, can create, list, revoke, or roll
/// over keys.
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
[Authorize(Roles = IdentityRoleNames.Admin)]
public class KeyController(IIdentityService identityService) : ControllerBase
{
    /// <summary>Issues a new API key. The raw key is returned exactly once, in this response — it is
    /// never shown again, only its hash is persisted.</summary>
    [HttpPost]
    public async Task<ActionResult<CreateApiKeyResponseDto>> Create(CreateApiKeyRequestDto request, CancellationToken cancellationToken)
    {
        var response = await identityService.CreateApiKeyAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Lists issued API keys (metadata only). Pass <c>includeRevoked=true</c> to also see
    /// revoked keys.</summary>
    [HttpGet]
    public async Task<ActionResult<ListApiKeysResponseDto>> List([FromQuery] bool includeRevoked = false, CancellationToken cancellationToken = default)
    {
        var response = await identityService.ListApiKeysAsync(new ListApiKeysRequestDto { IncludeRevoked = includeRevoked }, cancellationToken);
        return Ok(response);
    }

    /// <summary>Revokes a key by id. Permanent — a revoked key cannot be restored, only replaced by
    /// creating a new one.</summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<RevokeApiKeyResponseDto>> Revoke(string id, CancellationToken cancellationToken)
    {
        var response = await identityService.RevokeApiKeyAsync(new RevokeApiKeyRequestDto { KeyId = id }, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }

    /// <summary>Rolls a key over: generates a brand new secret for it while keeping its id, name, contact
    /// info, expiry and usage history. The old raw key stops working immediately. Fails on an unknown or
    /// already-revoked id — a revoked key must be replaced with a new one instead.</summary>
    [HttpPost("{id}")]
    public async Task<ActionResult<RolloverApiKeyResponseDto>> Rollover(string id, CancellationToken cancellationToken)
    {
        var response = await identityService.RolloverApiKeyAsync(new RolloverApiKeyRequestDto { KeyId = id }, cancellationToken);
        return response.Success ? Ok(response) : NotFound(response);
    }
}

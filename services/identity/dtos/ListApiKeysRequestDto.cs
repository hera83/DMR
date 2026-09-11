namespace DMR.Services.Identity.Dtos;

/// <summary>Request for <c>IIdentityService.ListApiKeysAsync</c>.</summary>
public class ListApiKeysRequestDto
{
    /// <summary>When false (the default), revoked keys are left out of the result.</summary>
    public bool IncludeRevoked { get; set; }
}

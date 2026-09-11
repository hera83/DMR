namespace DMR.Services.Identity.Dtos;

/// <summary>Response for <c>IIdentityService.ListApiKeysAsync</c>.</summary>
public class ListApiKeysResponseDto
{
    public List<ApiKeyDto> Keys { get; set; } = [];
}

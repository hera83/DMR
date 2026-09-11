namespace DMR.Services.Logs.Dtos;

/// <summary>
/// Search/filter parameters for <see cref="Interfaces.ILogsService.SearchLogsAsync"/>. Bound from the
/// query string on <c>GET /Log/Search</c> — see controllers/LogController.cs. Every filter is optional and
/// filters combine with AND.
/// </summary>
public class SearchLogsRequestDto
{
    /// <summary>Restrict results to these Serilog levels (matched case-insensitively, e.g. "Warning",
    /// "Error"). Bind as a repeated query parameter (<c>?levels=Warning&amp;levels=Error</c>). Leave
    /// empty/null to include every level.</summary>
    public List<string>? Levels { get; set; }

    /// <summary>Only include events logged at or after this UTC instant.</summary>
    public DateTime? FromUtc { get; set; }

    /// <summary>Only include events logged at or before this UTC instant.</summary>
    public DateTime? ToUtc { get; set; }

    /// <summary>Case-insensitive substring match against the rendered message and the exception text.</summary>
    public string? SearchText { get; set; }

    /// <summary>True to only return events that carried an exception, false to only return events that
    /// didn't. Leave null to include both.</summary>
    public bool? HasException { get; set; }

    /// <summary>1-based page number. Values below 1 are treated as 1. Defaults to 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Rows per page. Defaults to 50; clamped to [1, 500] by <c>LogsService</c> regardless of what
    /// a caller passes.</summary>
    public int PageSize { get; set; } = 50;
}

namespace DMR.Services.Logs.Dtos;

/// <summary>Result of a log search — one page of matching rows, newest first, plus the total match count
/// across every page.</summary>
public class SearchLogsResponseDto
{
    /// <summary>Total number of rows matching the filters, across every page (not just this one).</summary>
    public int TotalCount { get; set; }

    /// <summary>1-based page number this response contains.</summary>
    public int Page { get; set; }

    /// <summary>Rows per page actually used, after clamping the request's value.</summary>
    public int PageSize { get; set; }

    /// <summary>Matching rows for this page, newest first.</summary>
    public List<LogEntryDto> Items { get; set; } = [];
}

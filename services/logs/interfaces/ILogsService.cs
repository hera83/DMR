using DMR.Services.Logs.Dtos;

namespace DMR.Services.Logs.Interfaces;

/// <summary>
/// Searches the Serilog SQLite log store (see services/logs/docs) — the single "Logs" table every request
/// across the whole app writes to via the Serilog SQLite sink configured in Program.cs. Purely read-only:
/// this service never writes to the log store itself.
/// </summary>
public interface ILogsService
{
    /// <summary>Searches logged events with optional level/time-range/text/exception filters, returned
    /// newest first and paginated.</summary>
    Task<SearchLogsResponseDto> SearchLogsAsync(SearchLogsRequestDto request, CancellationToken cancellationToken = default);
}

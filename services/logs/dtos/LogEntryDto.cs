namespace DMR.Services.Logs.Dtos;

/// <summary>
/// A single row from the Serilog SQLite log store (see services/logs/docs). Exception to the
/// request/response pairing rule: a supporting item type nested inside <see cref="SearchLogsResponseDto"/>,
/// never sent on its own.
/// </summary>
public class LogEntryDto
{
    /// <summary>Autoincrement row id from the underlying "Logs" table.</summary>
    public long Id { get; set; }

    /// <summary>UTC instant the event was logged at.</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>Serilog level as text (e.g. "Verbose", "Debug", "Information", "Warning", "Error",
    /// "Fatal").</summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>The rendered log message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Exception details (including stack trace) when the event carried one, otherwise
    /// null.</summary>
    public string? Exception { get; set; }

    /// <summary>Structured log properties the sink serialized as JSON text, or null when the event carried
    /// none. Returned as-is (not parsed) — deserialize it yourself if you need individual property
    /// values.</summary>
    public string? Properties { get; set; }
}

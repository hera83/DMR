using System.Globalization;
using DMR.Services.Logs.Dtos;
using DMR.Services.Logs.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;

namespace DMR.Services.Logs;

/// <inheritdoc cref="ILogsService"/>
public class LogsService : ILogsService
{
    /// <summary>Name of the table the Serilog SQLite sink writes to — must stay in sync with Program.cs's
    /// <c>WriteTo.SQLite(tableName: ...)</c> call.</summary>
    private const string TableName = "Logs";

    /// <summary>Format the sink stores the <c>Timestamp</c> column as (see Program.cs —
    /// <c>storeTimestampInUtc: true</c>), e.g. "2026-09-11T14:03:27.512". Fixed-width and zero-padded, so
    /// plain string comparison sorts/filters it correctly — used here to format range-filter bounds
    /// identically before comparing.</summary>
    private const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fff";

    /// <summary>Hard ceiling on <see cref="SearchLogsRequestDto.PageSize"/> so a caller can't force a
    /// full-table scan/response in one request.</summary>
    private const int MaxPageSize = 500;

    private readonly IHostEnvironment _environment;
    private readonly ILogger<LogsService> _logger;

    public LogsService(IHostEnvironment environment, ILogger<LogsService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<SearchLogsResponseDto> SearchLogsAsync(SearchLogsRequestDto request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 50 : request.PageSize, 1, MaxPageSize);

        var whereClauses = new List<string>();
        var parameters = new List<(string Name, object Value)>();

        if (request.Levels is { Count: > 0 })
        {
            var placeholders = new List<string>();
            for (var i = 0; i < request.Levels.Count; i++)
            {
                var name = $"@level{i}";
                placeholders.Add(name);
                parameters.Add((name, request.Levels[i]));
            }
            // COLLATE NOCASE — levels are matched case-insensitively ("warning" == "Warning").
            whereClauses.Add($"Level COLLATE NOCASE IN ({string.Join(", ", placeholders)})");
        }

        if (request.FromUtc is { } fromUtc)
        {
            whereClauses.Add("Timestamp >= @fromUtc");
            parameters.Add(("@fromUtc", fromUtc.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture)));
        }

        if (request.ToUtc is { } toUtc)
        {
            whereClauses.Add("Timestamp <= @toUtc");
            parameters.Add(("@toUtc", toUtc.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            whereClauses.Add("(RenderedMessage LIKE @searchText OR Exception LIKE @searchText)");
            parameters.Add(("@searchText", $"%{request.SearchText}%"));
        }

        if (request.HasException is { } hasException)
        {
            whereClauses.Add(hasException
                ? "(Exception IS NOT NULL AND Exception <> '')"
                : "(Exception IS NULL OR Exception = '')");
        }

        var whereSql = whereClauses.Count > 0 ? $"WHERE {string.Join(" AND ", whereClauses)}" : string.Empty;

        _logger.LogDebug(
            "Searching logs: levels={Levels}, fromUtc={FromUtc}, toUtc={ToUtc}, hasException={HasException}, page={Page}, pageSize={PageSize}",
            request.Levels, request.FromUtc, request.ToUtc, request.HasException, page, pageSize);

        await using var connection = new SqliteConnection($"Data Source={GetDatabasePath()}");
        await connection.OpenAsync(cancellationToken);

        // The Serilog sink (see Program.cs) may be mid-write when a search runs — wait for its lock
        // instead of failing the request outright with "database is locked".
        await ExecuteNonQueryAsync(connection, "PRAGMA busy_timeout = 5000;", cancellationToken: cancellationToken);

        await EnsureSchemaAsync(connection, cancellationToken);

        var totalCount = Convert.ToInt32(await ExecuteScalarAsync(
            connection, $"SELECT COUNT(*) FROM {TableName} {whereSql}", parameters, cancellationToken));

        var items = new List<LogEntryDto>();
        await using (var selectCommand = connection.CreateCommand())
        {
            selectCommand.CommandText = $"""
                SELECT id, Timestamp, Level, RenderedMessage, Exception, Properties
                FROM {TableName}
                {whereSql}
                ORDER BY Timestamp DESC, id DESC
                LIMIT @pageSize OFFSET @offset
                """;
            foreach (var (name, value) in parameters)
                selectCommand.Parameters.AddWithValue(name, value);
            selectCommand.Parameters.AddWithValue("@pageSize", pageSize);
            selectCommand.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new LogEntryDto
                {
                    Id = reader.GetInt64(0),
                    TimestampUtc = DateTime.SpecifyKind(
                        DateTime.ParseExact(reader.GetString(1), TimestampFormat, CultureInfo.InvariantCulture),
                        DateTimeKind.Utc),
                    Level = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Message = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Exception = NullIfEmpty(reader.IsDBNull(4) ? null : reader.GetString(4)),
                    Properties = NullIfEmpty(reader.IsDBNull(5) ? null : reader.GetString(5)),
                });
            }
        }

        return new SearchLogsResponseDto
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = items,
        };
    }

    /// <summary>Creates the "Logs" table/indexes if they don't exist yet. Idempotent, and deliberately
    /// matches the exact schema the Serilog SQLite sink itself creates (see services/logs/docs) — this
    /// just means a search never 500s on a database no log event has been flushed to yet. The indexes are
    /// this service's own addition (the sink doesn't create any) so filtering/ordering by time or level
    /// stays fast as the table grows.</summary>
    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(connection, $"""
            CREATE TABLE IF NOT EXISTS {TableName} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT,
                Level VARCHAR(10),
                Exception TEXT,
                RenderedMessage TEXT,
                Properties TEXT
            )
            """, cancellationToken: cancellationToken);

        await ExecuteNonQueryAsync(connection,
            $"CREATE INDEX IF NOT EXISTS idx_{TableName}_timestamp ON {TableName}(Timestamp)", cancellationToken: cancellationToken);
        await ExecuteNonQueryAsync(connection,
            $"CREATE INDEX IF NOT EXISTS idx_{TableName}_level ON {TableName}(Level)", cancellationToken: cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection, string commandText, IEnumerable<(string Name, object Value)>? parameters = null, CancellationToken cancellationToken = default)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        foreach (var (name, value) in parameters ?? [])
            command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<object?> ExecuteScalarAsync(
        SqliteConnection connection, string commandText, IEnumerable<(string Name, object Value)> parameters, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    /// <summary>Fixed path — always ".\app_dbs\serilog.db", the single database the Serilog SQLite sink
    /// writes to (see Program.cs). Not configurable per request; see services/logs/docs. Must stay in sync
    /// with the path Program.cs passes to <c>WriteTo.SQLite(sqliteDbPath: ...)</c>.</summary>
    private string GetDatabasePath() =>
        Path.Combine(_environment.ContentRootPath, "app_dbs", "serilog.db");
}

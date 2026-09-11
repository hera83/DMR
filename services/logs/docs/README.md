# Logs service

Read-only search over the app's structured logs. It doesn't produce logs itself — every `ILogger<T>` call
anywhere in the app is captured by Serilog (wired up in `Program.cs`), and this service just queries the
SQLite table Serilog writes them to. See "Service structure conventions" in the repo root `CLAUDE.md` for
the general shape this follows.

## Why this is a service of its own, not part of `identity`

Searching logs is its own concern (a read path over a SQLite table), separate from authenticating requests.
It only *depends on* the `Admin` role that `identity` defines (see "Access control" below), the same way
`KeyController` does — it doesn't own or duplicate any identity logic.

## Files

- `interfaces/ILogsService.cs` — the one interface.
- `LogsService.cs` — the one implementation (service folder root, next to `docs/`, `dtos/`,
  `interfaces/`, not nested).
- `dtos/SearchLogsRequestDto.cs` / `dtos/SearchLogsResponseDto.cs` — the request/response pair for
  `SearchLogsAsync`.
- `dtos/LogEntryDto.cs` — a single result row. Exception to the request/response pairing rule: a
  supporting item type nested inside `SearchLogsResponseDto`, never sent on its own.

## Where the data comes from — Serilog, not this service

`Program.cs` configures Serilog with two sinks:

- **Console** — plain text to stdout, same as before.
- **SQLite** ([`Serilog.Sinks.SQLite`](https://github.com/saleem-mirza/serilog-sinks-sqlite)) — every log
  event across the whole app (including one line per HTTP request, via
  `app.UseSerilogRequestLogging()`) lands in a `Logs` table with columns `id`, `Timestamp`, `Level`,
  `Exception`, `RenderedMessage`, `Properties` (structured properties as JSON text).

`LogsService` opens that same SQLite file directly with `Microsoft.Data.Sqlite` and runs plain parameterized
SQL against the `Logs` table — there's no EF Core model for it (it isn't part of the shared
`AppIdentityDbContext`/`data/` layer; it's a fixed, sink-owned file, same relationship `dmr_<date>.db` has
to `DmrService`). It also creates the table and two indexes (`Timestamp`, `Level`) itself with
`CREATE ... IF NOT EXISTS` on every search — idempotent, matches the sink's own schema exactly, and just
means a search never fails on a fresh database no log event has reached yet.

## Database

SQLite at `app_dbs/serilog.db` — a **fixed** path, resolved independently in both `Program.cs` (for the
Serilog sink) and `LogsService.GetDatabasePath()` (for querying it), the same pattern `identity.db` and
`dmr_<date>.db` use elsewhere in this app. **Exactly one file, always** — per the project owner's explicit
requirement, the sink is configured with `rollOver: false` and a generous `maxDatabaseSize` (1 GB) so it
never creates a `serilog-yyyyMMdd_HHmmss-<guid>.db` sibling the way the sink can by default. A
`retentionPeriod` of 90 days is what actually keeps that single file bounded long-term (older rows are
pruned by the sink on a timer) — without it, "never roll over" would eventually mean "hit the size cap and
silently start dropping new log events", which defeats the point of logging in the first place.

`batchSize: 1` on the sink means every event is flushed immediately rather than buffered — a trade against
raw write throughput, made deliberately so a just-logged event shows up in a search right away. Revisit if
log volume ever makes that a bottleneck.

## Configuration

Log **level** is the one thing that's meant to differ per environment, so it lives in the `"Serilog"`
section of `appsettings.json`/`appsettings.Development.json` (read via
`Serilog.Settings.Configuration`'s `ReadFrom.Configuration`), replacing the old plain `"Logging"` section:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

`appsettings.Development.json` overrides `Default` down to `Debug` (with `Microsoft.AspNetCore` /
`Microsoft.EntityFrameworkCore` kept at `Information` there so framework/EF chatter doesn't flood the log
just because the app's own `Debug` logs are now visible).

Everything else about the sinks — the SQLite file's path/table name, `rollOver`, `maxDatabaseSize`,
`retentionPeriod`, `batchSize` — is fixed in code in `Program.cs`, **not** configurable via `appsettings`,
per the project's "fixed values aren't config/request fields" convention. Only the minimum level was asked
to differ per environment; the storage shape shouldn't.

## Access control

`LogController` carries `[Authorize(Roles = IdentityRoleNames.Admin)]` — exactly like `KeyController` — so
only the configured **master key** can search logs. No regular API key, however privileged, can reach it.
This is deliberate: logs can contain request details across every tenant/caller, not just the caller doing
the searching.

## Registration

Wired up in `Program.cs`:

```csharp
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.SQLite(sqliteDbPath: logDatabasePath, tableName: "Logs", storeTimestampInUtc: true,
        batchSize: 1, maxDatabaseSize: 1024, rollOver: false, retentionPeriod: TimeSpan.FromDays(90)));

builder.Services.AddScoped<ILogsService, DMR.Services.Logs.LogsService>();
// ...
app.UseSerilogRequestLogging(); // first in the pipeline, before UseSwagger
```

## Usage

Master key only:

```
GET /Log/Search?levels=Warning&levels=Error&fromUtc=2026-09-01T00:00:00Z&searchText=timeout&page=1&pageSize=50
X-Api-Key: <master key>
```

All filters are optional and combine with AND:

- `levels` — repeat the query param per level (`?levels=Warning&levels=Error`), matched case-insensitively.
- `fromUtc` / `toUtc` — inclusive UTC range.
- `searchText` — case-insensitive substring match against the rendered message and the exception text.
- `hasException` — `true`/`false` to filter to events that did/didn't carry an exception.
- `page` (default 1) / `pageSize` (default 50, capped at 500).

Response: `{ "totalCount": 137, "page": 1, "pageSize": 50, "items": [ { "id": ..., "timestampUtc": ...,
"level": "Warning", "message": ..., "exception": null, "properties": "{...}" } ] }`, newest first.

## Known gaps / follow-ups

- No automated tests yet.
- `Properties` is returned as the raw JSON text the sink stored, not parsed into a structured object —
  deserialize it client-side if you need individual property values.
- No full-text index on `RenderedMessage`/`Exception` — `searchText` is a plain `LIKE '%...%'`, fine at the
  log volumes this app produces today but a table scan at much larger volumes. Revisit (e.g. SQLite FTS5)
  if that ever becomes slow.

# Health service

Keepalive/health-check endpoint: the standard "is this API up and how's it doing" fields (status, response
time, uptime, environment, version) plus one extra validation parameter specific to this app — how old the
newest ingested DMR dataset is, judged against Motorstyrelsen's **weekly** export cadence (see
`bgServices/DmrWorker.cs`, which polls the FTP feed four times a day but only ever finds new data about once
a week). Exposed anonymously as `GET /Health/Check` (see
[`HealthController`](../../../controllers/HealthController.cs)) — no `X-Api-Key` required, unlike every other
endpoint in the app, since this is meant to be pollable by external uptime monitors/load balancers that
typically can't attach one.

## Files

- `interfaces/IHealthService.cs` — the one interface, `CheckAsync`.
- `HealthService.cs` — the one implementation (service folder root, not nested).
- `dtos/HealthCheckResponseDto.cs` — the response. No paired request DTO: `CheckAsync` takes no parameters
  (there's nothing a caller could meaningfully supply for a health check), same precedent as
  `IIdentityService.EnsureSeededAsync` — noted as an exception to the request/response pairing rule in the
  file's own doc comment.
- `dtos/DmrDataFreshnessDto.cs` — the DMR data-freshness check, nested inside `HealthCheckResponseDto`.
  Exception to the pairing rule (noted in the file): a small supporting item type, never exchanged alone.
- `dtos/HealthStatus.cs` — the shared three-level severity enum (`Healthy`/`Warning`/`Unhealthy`), used by
  both `HealthCheckResponseDto.Status` (the overall result) and `DmrDataFreshnessDto.Status` (the freshness
  check alone). Exception to the pairing rule for the same reason as the other DTOs here.

## The DMR data-freshness check

`HealthService.CheckAsync` finds the newest `DmrDataset` row the same way `DmrService.LookupVehicleAsync`
does (`ORDER BY CreatedAtUtc DESC`, first) — a single indexed read against `AppIdentityDbContext`, never
opening the dataset's actual `app_dbs/dmr_<date>.db` file, so the whole check stays fast enough to poll
frequently.

The age that matters is `DmrDataset.SourceFileModifiedUtc` — the "Modified" date the FTP server reported for
the source XML export, i.e. when Motorstyrelsen actually produced that data — **not**
`DmrDataset.CreatedAtUtc` (when this app happened to finish converting it, which could lag the source date by
a while on a slow conversion). Three fixed thresholds (`HealthService.WarningThresholdDays`/
`StaleThresholdDays`, matching the same "fixed constant, not configuration" pattern as `DmrWorker`'s own
window/retention constants):

| Age of `SourceFileModifiedUtc` | `Status` | `Message` |
|---|---|---|
| < 8 days | `Healthy` | *(none)* |
| 8-13 days | `Warning` | "Afventer ny data fra Motorregisteret." |
| ≥ 14 days | `Unhealthy` | "Forældet data." |
| No dataset ingested yet, no `DmrWorkerRun` in progress | `Unhealthy` | "Der er endnu ikke modtaget data fra Motorregisteret." |
| No dataset ingested yet, but a `DmrWorkerRun` **is** in progress | `Warning` | "Henter data fra Motorregisteret for første gang." |

The 8-day floor gives one full extra day of slack past the weekly cadence before saying anything; the 14-day
ceiling means two missed weekly updates in a row — at that point it's not "the update hasn't landed yet", it
points at an actual problem (the FTP feed, or `DmrWorker` itself) worth someone looking at.

**The last two rows both cover "no dataset exists yet", and only differ in whether a `DmrWorkerRun` is
currently mid-run** (`CompletedAtUtc == null`) — `DmrWorker` only inserts a `DmrDataset` row once
`ConvertAsync` has fully succeeded (see `DmrWorker.RunOnceAsync`), so a fresh deploy's first, often
multi-hour, download/conversion leaves `DmrDatasets` empty for the whole time it runs. Without this extra
`DmrWorkerRuns` check, that entirely expected state would look identical to "nothing has ever run" and get
flagged `Unhealthy`/503 for no real reason. This can't tell a genuinely running first ingest apart from one
whose `DmrWorkerRun` row got stuck at `CompletedAtUtc == null` forever after a hard crash (see that type's
own doc comment) — a first ingest that stays `Warning` for implausibly long is worth checking via
`LogController`. See `DmrDataFreshnessDto.IngestInProgress`.

Whenever `Message` is set, `HealthService` also logs it (with the dataset's file name and exact age, where
there is one) — `LogWarning` for the two `Unhealthy` cases and the 8-13 day `Warning` case, `LogInformation`
for the "first ingest in progress" `Warning` case, since that one isn't actually a problem. Either way it
shows up in `LogController`'s searchable log history too, not just in this one response — the whole point of
"man kan ... samtidigt se hvordan det går med apiet" (see also `LogController` for the Admin-only searchable
log store this feeds).

## Status codes

`HealthController.Check` returns HTTP 503 when the overall `Status` is `Unhealthy` (so uptime
monitors/load balancers can alert on it the normal way), 200 otherwise — a `Warning` result is still a 200,
since the API itself is working fine; only the data behind it needs attention.

## Registration

```csharp
builder.Services.AddScoped<IHealthService, DMR.Services.Health.HealthService>();
```

No options to bind — nothing here is configurable per the project's "fixed values aren't request/config
fields" convention (see `WarningThresholdDays`/`StaleThresholdDays`).

## Usage

```csharp
var health = await healthService.CheckAsync();
// health.Status == HealthStatus.Warning
// health.DmrDataFreshness!.Message == "Afventer ny data fra Motorregisteret."
// health.DmrDataFreshness!.AgeInDays == 9.2
```

Or over HTTP: `GET /Health/Check` (no `X-Api-Key` needed) — 200 with the body above, or 503 if `Status` is
`Unhealthy`.

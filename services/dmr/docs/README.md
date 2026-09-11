# Dmr service

Converts Motorstyrelsen's DMR "ESStatistikListeModtag" vehicle export — a multi-GB XML file, unpacked from
`app_files/downloads/*.zip` via [`ZipExtensions.Unpack`](../../../extensions/ZipExtensions.cs) into
`app_files/temp/ESStatistikListeModtag.xml` — into a local SQLite database, so the export's contents can be
looked up (by chassis number, plate number, vehicle id, ...) without re-parsing the XML every time. Also
serves those lookups: `IDmrService.LookupVehicleAsync` finds a vehicle by registration number in the newest
converted database, and [`DmrController`](../../../controllers/DmrController.cs) exposes it as
`GET /Dmr/GetVehicleAsync/{registreringNummer}`.

## Why a real database instead of just querying the XML

The unpacked XML is enormous (~120 GB for the sample export in this repo, streamed from a ~6.7 GB zip — see
[`extensions/ZipExtensions.cs`](../../../extensions/ZipExtensions.cs) and the note there about a `byte[]`
never being able to hold more than ~2 GB). It's fine to *stream through* once, but useless to query directly.
`DmrService.ConvertAsync` walks it exactly once, one `<Statistik>` (vehicle) element at a time, and writes
each into a date-stamped `app_dbs/dmr_<yyyy-MM-dd>.db` (e.g. `dmr_2026-09-06.db`) — the folder CLAUDE.md
already reserves for local database files.

## Files

- `interfaces/IDmrService.cs` — the one interface, `ConvertAsync` and `LookupVehicleAsync`.
- `DmrService.cs` — the one implementation (service folder root, not nested). Contains the SQLite schema
  (as data-driven column lists so `CREATE TABLE` and `INSERT` can never drift apart), the streaming
  XML→SQLite import pipeline (see "Performance tuning" below), the `LookupVehicleAsync` read path, and a
  private nested `VehicleBatchWriter` that owns the prepared, reusable insert/delete commands and
  transaction batching. The batch writer is a nested class rather than its own file specifically to keep
  this the service's single implementation file per the project's service structure conventions.
- `dtos/DmrConvertRequestDto.cs` / `dtos/DmrConvertResponseDto.cs` — the public request/response pair for
  `ConvertAsync`.
- `dtos/DmrLookupVehicleRequestDto.cs` / `dtos/DmrLookupVehicleResponseDto.cs` — the public request/response
  pair for `LookupVehicleAsync`.
- `dtos/DmrVehicleDto.cs` — the looked-up vehicle, mirroring the `Koeretoej` table's columns 1:1, plus its
  six child-table collections (`Anvendelser`, `SupplerendeKarrosserier`, `Drivmidler`, `Udstyr`,
  `BlokeringAarsager`, `Tilladelser`), each a small item-type DTO of its own
  (`dtos/DmrVehicle<Table>Dto.cs`). All of these are exceptions to the request/response pairing rule (noted
  in each file) — supporting item types nested inside `DmrLookupVehicleResponseDto`, never exchanged alone.
- `dtos/Xml/*.cs` — ~40 small classes, one per file, that mirror the export's XML shape 1:1 for
  `XmlSerializer`-based deserialization (see "XML shape DTOs" below). Exception to the request/response
  pairing rule noted in each file's doc comment: these are internal deserialization targets, never
  exchanged on a service call themselves.

## Fixed paths, not request fields

Both the source XML path and the destination database path are fixed constants resolved from
`IHostEnvironment.ContentRootPath` in `DmrService` (same pattern as `FtpService`'s fixed downloads
directory — see `services/ftp/docs`):

- Source: `.\app_files\temp\ESStatistikListeModtag.xml` — wherever `ZipExtensions.Unpack` always lands
  the export.
- Destination: `.\app_dbs\dmr_<yyyy-MM-dd>.db`, date-stamped with the **source XML file's own last-write
  time** (`File.GetLastWriteTime` — preserved from the zip entry's timestamp through `ZipExtensions.Unpack`,
  i.e. when Motorstyrelsen actually produced that data), not the date `ConvertAsync` happens to run on. A
  conversion that runs late, is re-run, or straddles midnight still gets the same, correct filename for the
  data it holds. Rebuilt from scratch on every `ConvertAsync` call that resolves to the same filename (any
  existing file with that name, plus its `-wal`/`-shm`, is deleted first, so re-running against the same
  source file can never mix with stale data). A source file with a different last-write date gets its own
  new dated file and leaves other dates' databases untouched — so `app_dbs/` can hold a dated history of
  imports rather than only ever the latest one. Because the existence check on the source file has to run
  before this can be computed, `ConvertAsync` checks `File.Exists(sourcePath)` first and only then calls
  `GetDatabasePath(sourcePath)` — see there for why the two can't be independent one-liners any more.

Neither is a field on `DmrConvertRequestDto` — callers can't redirect the conversion to arbitrary files.
The one real per-call option is `MaxRecords`, an optional cap useful for a quick smoke test on a subset
instead of committing to the full multi-hour run.

## XML shape DTOs (`dtos/Xml/`)

The export uses a single XML namespace (`http://skat.dk/dmr/2007/05/31/`, held as
`DmrXmlNamespace.Value`) and a deeply nested, mostly-optional schema — 169 distinct element names were
found across a 6,000+ vehicle sample. `DmrService` positions an `XmlReader` on each `<Statistik>` element
in turn (`ReadToFollowing` + `ReadSubtree`) and deserializes just that one vehicle via
`XmlSerializer.Deserialize(StatistikXml)` — the document itself is never loaded into memory, only one
vehicle at a time.

`XmlSerializer` silently ignores any XML element it has no mapped property for (it does not throw), so the
import is forward-compatible with export fields not covered here — they're simply not written to the
database rather than aborting the whole import.

### Cardinality decisions (why some things are columns and others are child tables)

Determined empirically against a 6,258-vehicle / 50 MB sample (see git history / conversation for the
analysis) before writing the schema:

| Structure | Observed max per vehicle | Modeled as |
|---|---|---|
| Farve (color), Syn (inspection) | 1 | flat columns on `Koeretoej` |
| KoeretoejAnvendelse (usage) | 1 primary + an optional fuller list | primary usage flattened; the list (`KoeretoejAnvendelseSamlingStruktur`, only present for multi-usage vehicles) → `KoeretoejAnvendelse` child table |
| Drivmiddel (fuel/power source) | 2 (e.g. hybrid) | `KoeretoejDrivmiddel` child table |
| Tilladelse (permit) | 4 | `KoeretoejTilladelse` child table |
| SupplerendeKarrosseri (suppl. body type) | 4 | `KoeretoejSupplerendeKarrosseri` child table |
| KoeretoejUdstyr (equipment) | 26 | `KoeretoejUdstyr` child table |
| BlokeringAarsag (blocking reason) | 1 in every sample seen | `KoeretoejBlokeringAarsag` child table anyway (kept as a list — see Known gaps) |

## Per-record resilience

`XmlSerializer` silently ignoring unmapped *elements* (see above) doesn't cover every surprise a ~120 GB,
only-ever-sampled export can contain — a mapped element can still hold a value that doesn't fit its
property's type. This happened for real on the first full run: `KoeretoejOplysningTraekkendeAksler` was
assumed to be a plain axle count (`int?`) from the sample, but turned out to sometimes hold a
comma-separated list of driven axle positions (e.g. `"1, 2"` for an all-wheel-drive vehicle) further into
the file — which threw partway through deserializing that one `<Statistik>` element. Fixed for that specific
field by widening it to `string?` (matching how `AkselAfstand` was already handled defensively), but since
more such surprises are plausible in the ~99%+ of the file no one has read, `ImportToSqlite`'s per-record
loop now catches any deserialization failure, logs it, and skips just that one vehicle rather than aborting
a run that may already be an hour deep — disposing the subtree `XmlReader` still correctly advances past
the failed record either way. Skipped counts are surfaced on `DmrConvertResponseDto.RecordsSkipped` and
logged as warnings (with a summary line at the end); worth periodically checking for a recurring field that
should get the same widening treatment `TraekkendeAksler` got.

Found the same way a second time, via `DmrConvertTest` (see "Known gaps" below): `KoeretoejMotorInnovativTeknikAntal`
was assumed to be a count (`int?`) from its name, but is actually the CO2 savings (g/km) from an
"eco-innovation" — a real decimal like `"0.8"`. Widened to `decimal?`/`REAL` (matching `SlagVolumen`/
`StoersteEffekt`, the other decimal fields on the same struct). This one alone was silently dropping ~1.3%
of every run before the fix — worth rechecking after any further test run whether any other `int?` column
shows up in the skip warnings.

## Known gaps / assumptions

- **`KoeretoejBlokeringAarsagListe` cardinality is inferred, not confirmed.** Only a single blocking
  reason was ever observed nested inside it in the sample. It's still modeled as a list (`List<KoeretoejBlokeringAarsagXml>`)
  in case a vehicle can carry more than one — if that assumption about the nesting is wrong, extra reasons
  beyond the first would simply not appear, silently. No crash either way.
- **`TilladelseTypeDetaljeValg` only has one known variant mapped**: `VariabelKombination` (a reference to
  a coupled vehicle's `KoeretoejIdent`, written to `KoeretoejTilladelse.DetaljeReferenceKoeretoejIdent`).
  Other permit types may carry other detail shapes under this "choice" element; unmapped ones are silently
  dropped (again, `XmlSerializer` ignores unknown elements rather than failing).
- **No PII in this export.** The sample was checked for owner/person-shaped fields (Ejer/Bruger/Cpr/Cvr/
  Adresse/Person) — none exist. This is a vehicle-technical-only extract.
- **`LookupVehicleAsync` only looks up by `RegistreringNummer`.** `StelNummer` is indexed too
  (`IX_Koeretoej_StelNummer`) but has no lookup method yet — a natural next addition if a chassis-number
  endpoint is ever needed, following the same `ReadKoeretoej`/`ReadChildRows` pattern.
- **`KoeretoejIdent` is not actually unique across the full export.** Confirmed on a real full run
  (2026-09-10): a dense burst of a few thousand repeated `KoeretoejIdent` values several million records
  into the ~120 GB file, mixed with some genuinely malformed XML in the same stretch — none of which showed
  up in the 3-vehicle test fixture. `VehicleBatchWriter.Add` now catches the resulting
  `SQLITE_CONSTRAINT`/error-19 violation specifically (see `SqliteConstraintViolationErrorCode`) and treats
  it as "last occurrence wins": it deletes the vehicle's existing `Koeretoej` row *and every child-table
  row* for that `KoeretoejIdent`, then re-inserts the new occurrence fresh, so the two occurrences' child
  rows never end up mixed together. This only runs on the rare duplicate path — the normal one-row-per-
  vehicle path pays nothing extra. Whether the *earlier* occurrence would ever be the more "correct" one is
  unknown; "last wins" was picked as the simpler, less surprising default (the file is read top to bottom,
  so this matches "newest information in the file overwrites older").
- **Windows `EventLog` is throttled to `Error` for this app (see the root `appsettings.json` `Logging`
  section) — this is what actually turned the incident above into an overnight stall, not the duplicate
  records by themselves.** ASP.NET Core adds the `EventLog` provider by default on Windows, and every
  `LogWarning`/`LogError` call through it is a synchronous, cross-process write to the Windows Event Log —
  dramatically slower than `Console`/`Debug` (observed: ~35-40 ms per call, capping throughput at roughly 27
  warnings/second during the burst above). `ImportToSqlite`'s per-record `catch` intentionally logs every
  skipped record (see "Per-record resilience" below) — necessary for diagnosing exactly this kind of issue,
  but expensive at `EventLog`'s default `Warning` level once skips happen at real volume on a multi-GB file.
  `Console`/`Debug` still receive `Information`+ as before; only `EventLog` was raised to `Error`, so a
  genuine crash/fatal error still reaches Event Viewer.
- **Full-file verification was manual, not part of the automated suite.** The real ~120 GB export takes
  minutes to import and only exists on the machine it was downloaded to — `dmr.tests/DmrServiceTests.cs`
  runs the same code against a small 3-vehicle fixture (`dmr.tests/TestData/DmrSample.xml`, a verbatim
  excerpt of the real export) instead.

## Performance tuning

`ConvertAsync`'s parse/insert pipeline is synchronous internally (`XmlSerializer` and `Microsoft.Data.Sqlite`
are both sync APIs) and can run for a long time against the full export — the whole thing is offloaded via
`Task.Run` so it never blocks the calling thread, but **don't await it inside an HTTP request** for the same
reason noted in `services/ftp/docs` for multi-hour FTP downloads: kick it off as a background job and let
the endpoint return immediately.

`ImportToSqlite`'s biggest lever, added 2026-09-10 after a real full run took far longer than expected (see
"Known gaps" for what actually happened): **parsing and writing run on two separate threads**, handed off
through a bounded `BlockingCollection<(int SeenNumber, StatistikXml Vehicle)>` (`ParseQueueCapacity`
entries). `XmlSerializer` deserialization is CPU-bound, reflection-heavy work; the SQLite writes are a
different mix of CPU and disk I/O — strictly alternating the two on one thread (the old shape) means each
one idles while the other runs. Splitting them lets the parser keep reading ahead while the writer is mid-
transaction, and vice versa. The queue is *bounded*, not unbounded, so a temporarily slow writer applies
backpressure to the parser (blocks its `queue.Add`) instead of buffering the whole file's vehicles in
memory. Only the writer thread ever touches the `SqliteConnection`/`VehicleBatchWriter` — it isn't safe for
concurrent use from multiple threads, so the parser thread never touches either, only the queue.

Everything else:
- **Batched transactions** (`BatchSize = 20_000` vehicles per `SqliteTransaction`, raised from an original
  5,000 — see "Known gaps") — committing per-row would be dramatically slower, and the larger the batch the
  fewer commits (and WAL checkpoints) the whole import pays for.
- **Prepared, reused `SqliteCommand`s** — one `INSERT`/`DELETE` command per table, built once, with
  parameter values just overwritten per row instead of building a new command/parameter set every time. The
  `DELETE`s are only ever used by the duplicate-`KoeretoejIdent` path (see "Known gaps").
- **`PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;`** — skips most of the per-transaction fsync cost
  the default rollback-journal/`FULL` mode pays for, which matters across many batched transactions.
- **`PRAGMA cache_size=-200000; PRAGMA temp_store=MEMORY; PRAGMA mmap_size=268435456;`** (added alongside
  the pipelining change above) — SQLite's default page cache is only 2 MB, far too small to keep a
  meaningful fraction of a multi-GB import's working set (index pages especially) in memory; raising it to
  ~200 MB cuts down on re-reading pages from disk across the many batched transactions. `temp_store=MEMORY`
  keeps any temp b-trees SQLite needs — including the ones `CreateLookupIndexes` builds afterward — off disk
  too.
- **Lookup indexes deferred until after the import, not created up front.** `CreateTables` only creates the
  child-table `KoeretoejIdent` indexes (load-bearing during the import itself — see "Known gaps") along with
  the tables; `Koeretoej`'s own two lookup indexes (`RegistreringNummer`, `StelNummer` — used only by
  `LookupVehicleAsync`, never during the import) are built once by `CreateLookupIndexes`, after every row is
  already in. Building an index once over a full table is far cheaper than incrementally maintaining its
  B-tree on every one of ~20 million individual inserts.
- **`Pooling=False`** on the connection string — this is a single long-lived connection for one big import,
  not a high-frequency pooled scenario, and pooling would otherwise keep the native SQLite handle alive past
  `Dispose()`, causing the *next* `ConvertAsync` run's `File.Delete(databasePath)` to fail with the file
  still locked (hit this exact issue writing `DmrServiceTests` — see its `SqliteConnection.ClearAllPools()`
  comment for the test-side half of the same problem).

## Looking up a vehicle (`LookupVehicleAsync`)

Unlike `ConvertAsync`, this is fast — one indexed `SELECT ... WHERE RegistreringNummer = @x COLLATE NOCASE`
against `Koeretoej`, plus one small `WHERE KoeretoejIdent = @id` query per child table — so it's safe to
await directly inside an HTTP request. `DmrController` does exactly that.

`RegistreringNummer` is declared `TEXT COLLATE NOCASE` in `KoeretoejColumns` *and* the query still spells
out `COLLATE NOCASE` explicitly — both matter:
- The column-level `COLLATE NOCASE` is what lets `IX_Koeretoej_RegistreringNummer` (built by
  `CreateLookupIndexes` after every import — `RegistreringNummer` is this service's primary search
  parameter) actually get used for a case-insensitive match: SQLite only accelerates a `COLLATE NOCASE`
  comparison with an index whose own collating sequence agrees, and an index inherits its collation from the
  column it's built on. Before this, the query's `COLLATE NOCASE` had nothing to agree with, and
  `EXPLAIN QUERY PLAN` confirmed the fallback: `SCAN Koeretoej` — a full scan of tens of millions of rows on
  every single lookup.
- The query-level `COLLATE NOCASE` stays because it's what makes the lookup *correct* — costs nothing
  against a database built with the fix above (verified with `EXPLAIN QUERY PLAN`: a query-side
  `COLLATE NOCASE` that agrees with the column's still uses the index), but without it, any
  `app_dbs/dmr_<date>.db` imported by an older build of this service (column not yet `COLLATE NOCASE`)
  would silently start matching case-*sensitively* — dropping it is a real regression, not just a
  micro-optimisation, verified directly against the production `dmr_2026-09-06.db` in this repo (a plate
  matched with its original casing, failed to match with the casing flipped, once the query's `COLLATE
  NOCASE` was removed).

**An already-imported `app_dbs/dmr_<date>.db` built before this fix stays on the slow, full-table-scan path
until it's rebuilt** (the next `ConvertAsync`/`DmrWorker` run that produces a new dated file) — no
query-side change can retrofit an existing database's column collation; only a fresh import does, since
`ImportToSqlite` always rebuilds its output file from scratch (see "Fixed paths, not request fields").

1. Finds the newest `DmrDataset` row (`ORDER BY CreatedAtUtc DESC`, first) via `AppIdentityDbContext` — this
   is what "the current dataset" means everywhere in this service; see `DmrWorker` for how a row gets
   registered there and how old ones get pruned.
2. Opens the dataset's `DatabasePath` with `Mode=ReadOnly;Pooling=False` — read-only because this never
   writes, and `Pooling=False` for the exact same reason `ImportToSqlite`'s write connection uses it (see
   above): a pooled connection would keep the file locked past `Dispose()`, which could make `DmrWorker`'s
   retention cleanup fail to delete this database once it falls out of the retained set.
3. `ReadKoeretoej` reads the matching row (case-insensitive match on `RegistreringNummer`); `ReadChildRows<T>`
   is shared plumbing for the six child-table reads (`ReadAnvendelseList`, `ReadDrivmiddelList`, ...), each
   just supplying its table/columns/row-mapper.

`DmrService` therefore also depends on `AppIdentityDbContext` (constructor-injected, same pattern as
`IdentityService`) — this is the one part of the service that isn't only about the fixed XML/SQLite paths.

## Registration

```csharp
builder.Services.AddScoped<IDmrService, DMR.Services.Dmr.DmrService>();
```

No options to bind — there's nothing configurable (see "Fixed paths" above).

## Usage

```csharp
var result = await dmrService.ConvertAsync(new DmrConvertRequestDto());
// result.DatabasePath == ".\app_dbs\dmr_2026-09-09.db" (today's date)
// result.RecordsImported == number of <Statistik> vehicles written

var lookup = await dmrService.LookupVehicleAsync(new DmrLookupVehicleRequestDto { RegistreringNummer = "AB12345" });
// lookup.Vehicle == the matched vehicle (incl. child collections), from whichever DmrDataset is newest
// lookup.Success == false, with ErrorMessage set, if no dataset exists yet or no vehicle matched
```

Or over HTTP: `GET /Dmr/GetVehicleAsync/AB12345` (see `controllers/DmrController.cs`) — 200 with the vehicle, or 404 with
`ErrorMessage` explaining why.

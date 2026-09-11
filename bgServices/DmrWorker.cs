using DMR.Data;
using DMR.Data.Models;
using DMR.Extensions;
using DMR.Services.Dmr.Dtos;
using DMR.Services.Dmr.Interfaces;
using DMR.Services.Ftp.Dtos;
using DMR.Services.Ftp.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DMR.BgServices;

/// <summary>
/// Background worker that runs the daily DMR ingest: FTP list → download the newest export (if it isn't
/// already ingested) → unpack → convert to SQLite via <see cref="IDmrService"/> → register the result as
/// the active <see cref="DmrDataset"/> → prune old datasets → clear the download/temp scratch directories.
/// Registered as a hosted service in <c>Program.cs</c> (<c>builder.Services.AddHostedService&lt;DmrWorker&gt;()</c>).
///
/// ## Schedule — four fixed windows a day: 00:00, 06:00, 12:00, 18:00 local time
///
/// A run only counts toward "already done" if it *succeeded* (see <see cref="DmrWorkerRun.Succeeded"/>) and
/// completed on or after the start of the current fixed window — the most recent of 00:00/06:00/12:00/18:00
/// local time to have passed (<see cref="WindowLength"/>, <see cref="GetCurrentWindowStartUtc"/>). Each loop
/// iteration checks for a qualifying success:
/// - If one exists, sleep until the *next* fixed boundary (<see cref="GetDelayUntilNextWindow"/>) and check
///   again — a clock-aligned wait, not a floating <see cref="WindowLength"/> relative to whenever the last
///   run happened, so runs land on 00:00/06:00/12:00/18:00 on the dot instead of drifting later each day.
/// - If not, run immediately. A failed run does **not** count as "already done", so a transient failure
///   (FTP hiccup, etc.) is retried after <see cref="FailureRetryDelay"/> rather than blocking the rest of
///   the window — and a restart that missed a boundary entirely (app was down, or started later in the
///   window) catches up right away instead of waiting for the next one.
///
/// ## Skipping an unchanged export
///
/// After listing the FTP directory and sorting newest-first by <c>ModifiedUtc</c>, the newest file's
/// name + modified date are compared against the most recently registered <see cref="DmrDataset"/>. A
/// match means that exact file was already downloaded and converted, so the run short-circuits to a
/// success with <see cref="DmrWorkerRun.Message"/> = "Database is up to date." instead of re-downloading a
/// multi-GB file for nothing.
///
/// ## Dependency injection
///
/// <see cref="AppIdentityDbContext"/>/<see cref="IFtpService"/>/<see cref="IDmrService"/>/<see cref="IHostEnvironment"/>
/// are all scoped or otherwise unsuited to direct constructor injection into a singleton
/// <see cref="BackgroundService"/>, so this takes an <see cref="IServiceScopeFactory"/> instead and opens
/// one scope per iteration, same as the startup migration/seed block in <c>Program.cs</c>.
///
/// Every run is logged as one <see cref="DmrWorkerRun"/> row: inserted (and saved) when the run starts, so
/// a crash mid-run still leaves a visible row stuck at <c>Succeeded == null</c> instead of vanishing
/// silently, and updated with the outcome when the run finishes (or throws).
/// </summary>
public class DmrWorker : BackgroundService
{
    /// <summary>Length of one fixed run window — windows start at 00:00/06:00/12:00/18:00 local time; see
    /// the schedule notes above.</summary>
    private static readonly TimeSpan WindowLength = TimeSpan.FromHours(6);

    /// <summary>How long to sleep before retrying after a failed run, so a persistent problem (FTP server
    /// down, etc.) doesn't spin the loop in a tight retry storm.</summary>
    private static readonly TimeSpan FailureRetryDelay = TimeSpan.FromMinutes(15);

    /// <summary>Remote FTP directory the Motorstyrelsen export lands in.</summary>
    private const string RemoteDirectory = "ESStatistikListeModtag";

    /// <summary>How many <see cref="DmrDataset"/> rows (and their backing .db files) to keep — see
    /// <see cref="CleanupOldDatasetsAsync"/>.</summary>
    private const int MaxRetainedDatasets = 2;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DmrWorker> _logger;

    public DmrWorker(IServiceScopeFactory scopeFactory, ILogger<DmrWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

            if (await HasSucceededSinceCurrentWindowStartAsync(dbContext, stoppingToken))
            {
                var delay = GetDelayUntilNextWindow();
                _logger.LogInformation(
                    "DmrWorker: already succeeded for the current 6-hour window; next run at {NextRunLocal:yyyy-MM-dd HH:mm} local.",
                    DateTime.Now + delay);
                if (!await DelayAsync(delay, stoppingToken))
                    break; // shutdown requested while waiting — stop the loop.
                continue;
            }

            var succeeded = await RunOnceAsync(scope, dbContext, stoppingToken);
            if (!succeeded)
            {
                _logger.LogInformation("DmrWorker: run did not succeed; retrying in {Delay}.", FailureRetryDelay);
                if (!await DelayAsync(FailureRetryDelay, stoppingToken))
                    break;
            }

            // On success, looping straight back to the top re-evaluates HasSucceededSinceCurrentWindowStartAsync,
            // which will now be true and take the "sleep until next boundary" branch above — no separate
            // post-success delay needed here.
        }
    }

    /// <summary>True if a <see cref="DmrWorkerRun"/> already completed successfully on or after the start
    /// of the current fixed 6-hour window.</summary>
    private static async Task<bool> HasSucceededSinceCurrentWindowStartAsync(AppIdentityDbContext dbContext, CancellationToken stoppingToken)
    {
        var windowStartUtc = GetCurrentWindowStartUtc();
        return await dbContext.DmrWorkerRuns.AnyAsync(
            r => r.Succeeded == true && r.CompletedAtUtc != null && r.CompletedAtUtc >= windowStartUtc,
            stoppingToken);
    }

    /// <summary>Local start of the fixed 6-hour window <paramref name="now"/> falls in — floors its
    /// time-of-day down to the nearest multiple of <see cref="WindowLength"/>, giving exactly 00:00, 06:00,
    /// 12:00, or 18:00.</summary>
    private static DateTime GetCurrentWindowStartLocal(DateTime now)
    {
        var windowsSinceMidnight = now.TimeOfDay.Ticks / WindowLength.Ticks;
        return now.Date + TimeSpan.FromTicks(windowsSinceMidnight * WindowLength.Ticks);
    }

    /// <summary>Start (in UTC) of the fixed 6-hour window currently in progress — see
    /// <see cref="GetCurrentWindowStartLocal"/>.</summary>
    private static DateTime GetCurrentWindowStartUtc() =>
        GetCurrentWindowStartLocal(DateTime.Now).ToUniversalTime();

    /// <summary>How long to sleep until the next fixed window boundary (current window start +
    /// <see cref="WindowLength"/>) — a clock-aligned wait rather than a flat <see cref="WindowLength"/> from
    /// now, so repeated "already succeeded" checks don't drift the actual run times later each cycle.</summary>
    private static TimeSpan GetDelayUntilNextWindow()
    {
        var now = DateTime.Now;
        var nextWindowStartLocal = GetCurrentWindowStartLocal(now) + WindowLength;
        return nextWindowStartLocal - now;
    }

    /// <summary>Sleeps for <paramref name="delay"/>. Returns false (instead of throwing) if shutdown was
    /// requested while waiting, so callers can just break out of the loop.</summary>
    private static async Task<bool> DelayAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Runs the whole ingest pipeline once, as one logged <see cref="DmrWorkerRun"/>: list → compare
    /// against the newest registered <see cref="DmrDataset"/> → (skip, or download → unpack → convert →
    /// register → prune old datasets → clear scratch directories). Returns whether the run succeeded.
    /// </summary>
    private async Task<bool> RunOnceAsync(IServiceScope scope, AppIdentityDbContext dbContext, CancellationToken stoppingToken)
    {
        var ftpService = scope.ServiceProvider.GetRequiredService<IFtpService>();
        var dmrService = scope.ServiceProvider.GetRequiredService<IDmrService>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        var run = new DmrWorkerRun();
        dbContext.DmrWorkerRuns.Add(run);
        // Saved immediately (not just at the end) so a crash mid-run leaves a row visibly stuck at
        // Succeeded == null instead of the run vanishing entirely — see the class doc comment.
        await dbContext.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("DmrWorker: run {RunId} started at {StartedAtUtc:yyyy-MM-dd HH:mm:ss} UTC.", run.Id, run.StartedAtUtc);

        try
        {
            var fileListResult = await ftpService.ListFilesAsync(new FtpListFilesRequestDto { RemoteDirectory = RemoteDirectory }, stoppingToken);
            if (!fileListResult.Success)
            {
                _logger.LogError("DmrWorker: failed to list FTP directory {RemoteDirectory}. Error: {ErrorMessage}", RemoteDirectory, fileListResult.ErrorMessage);
                run.Succeeded = false;
                run.ErrorMessage = $"Failed to list FTP directory: {fileListResult.ErrorMessage}";
                return false;
            }

            // Newest file first, so the newest is what gets compared/downloaded below.
            var files = fileListResult.Files.OrderByDescending(f => f.ModifiedUtc).ToList();
            if (files.Count == 0)
            {
                _logger.LogWarning("DmrWorker: no files found in FTP directory {RemoteDirectory}.", RemoteDirectory);
                run.Succeeded = false;
                run.ErrorMessage = "No files found in FTP directory.";
                return false;
            }

            var latestFile = files[0];
            run.SourceFileName = latestFile.Name;
            run.SourceFileModifiedUtc = latestFile.ModifiedUtc;

            var latestDataset = await dbContext.DmrDatasets
                .OrderByDescending(d => d.CreatedAtUtc)
                .FirstOrDefaultAsync(stoppingToken);

            // Only treat it as "already ingested" when the server actually reports a modified date to
            // compare — without one there's no safe way to tell the file apart from a same-named file that
            // changed, so falling through to download it again is the safer default.
            if (latestFile.ModifiedUtc.HasValue && latestDataset is not null
                && latestDataset.SourceFileName == latestFile.Name
                && latestDataset.SourceFileModifiedUtc == latestFile.ModifiedUtc.Value)
            {
                _logger.LogInformation(
                    "DmrWorker: newest FTP file {FileName} (modified {ModifiedUtc:yyyy-MM-dd HH:mm:ss} UTC) already ingested as dataset {DatasetId}; database is up to date.",
                    latestFile.Name, latestFile.ModifiedUtc, latestDataset.Id);
                run.Succeeded = true;
                run.Message = "Database is up to date.";
                return true;
            }

            _logger.LogInformation("DmrWorker: downloading {FileName} from FTP directory {RemoteDirectory}.", latestFile.Name, RemoteDirectory);
            var downloadResult = await ftpService.DownloadFileAsync(new FtpDownloadFileRequestDto { RemotePath = latestFile.FullPath }, stoppingToken);
            if (!downloadResult.Success)
            {
                _logger.LogError("DmrWorker: failed to download file {FileName}. Error: {ErrorMessage}", latestFile.Name, downloadResult.ErrorMessage);
                run.Succeeded = false;
                run.ErrorMessage = $"Failed to download file: {downloadResult.ErrorMessage}";
                return false;
            }
            _logger.LogInformation("DmrWorker: downloaded {FileName} ({SizeBytes} bytes) to {LocalPath}.", latestFile.Name, downloadResult.SizeBytes, downloadResult.LocalPath);

            _logger.LogInformation("DmrWorker: unpacking {LocalPath}.", downloadResult.LocalPath);
            var extractedFiles = downloadResult.LocalPath.Unpack();
            _logger.LogInformation("DmrWorker: unpacked {Count} file(s) from {LocalPath}.", extractedFiles.Count, downloadResult.LocalPath);

            var convertResult = await dmrService.ConvertAsync(new DmrConvertRequestDto(), stoppingToken);
            if (!convertResult.Success)
            {
                _logger.LogError("DmrWorker: failed to convert DMR export. Error: {ErrorMessage}", convertResult.ErrorMessage);
                run.Succeeded = false;
                run.ErrorMessage = $"Failed to convert DMR export: {convertResult.ErrorMessage}";
                return false;
            }
            _logger.LogInformation(
                "DmrWorker: converted {RecordsImported} vehicle(s) ({RecordsSkipped} skipped) into {DatabasePath} in {Duration}.",
                convertResult.RecordsImported, convertResult.RecordsSkipped, convertResult.DatabasePath, convertResult.Duration);

            // DatabasePath is date-stamped from the source XML's own last-write time (see
            // DmrService.GetDatabasePath), not from "now" — so it's possible, if unlikely, for this
            // ConvertAsync call to have landed on the same path as an earlier dataset (e.g. the FTP source
            // re-publishing a file whose internal timestamp didn't move). ConvertAsync's ImportToSqlite
            // already deletes and rebuilds that file from scratch regardless, so any existing row still
            // pointing at the same path is now describing stale data — remove it before adding the fresh
            // row instead of leaving two rows pointing at one file (CleanupOldDatasetsAsync below prunes by
            // age, not by path, so it can't be relied on alone to catch this; see also the unique index on
            // DmrDataset.DatabasePath, which turns a missed case here into a loud failure instead of a
            // silent one).
            var supersededDatasets = await dbContext.DmrDatasets
                .Where(d => d.DatabasePath == convertResult.DatabasePath)
                .ToListAsync(stoppingToken);
            if (supersededDatasets.Count > 0)
            {
                dbContext.DmrDatasets.RemoveRange(supersededDatasets);
                _logger.LogInformation(
                    "DmrWorker: removing {Count} dataset row(s) that already pointed at {DatabasePath}; its file was just rebuilt from scratch.",
                    supersededDatasets.Count, convertResult.DatabasePath);
            }

            var dataset = new DmrDataset
            {
                SourceFileName = latestFile.Name,
                // ModifiedUtc was already required to be non-null for the "already ingested" comparison
                // above to have been reached at all when a prior dataset exists; when there's no prior
                // dataset yet (first-ever run) a missing ModifiedUtc still shouldn't block registering the
                // dataset, so fall back to "now" rather than throwing.
                SourceFileModifiedUtc = latestFile.ModifiedUtc ?? DateTime.UtcNow,
                DatabasePath = convertResult.DatabasePath,
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            dbContext.DmrDatasets.Add(dataset);
            await dbContext.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("DmrWorker: registered dataset {DatasetId} from {FileName} as active.", dataset.Id, latestFile.Name);

            await CleanupOldDatasetsAsync(dbContext, stoppingToken);
            ClearDownloadAndTempDirectories(environment);

            run.Succeeded = true;
            run.Message = $"Ingested new dataset from '{latestFile.Name}'.";
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            run.Succeeded = false;
            run.ErrorMessage = ex.Message;
            _logger.LogError(ex, "DmrWorker: run {RunId} failed.", run.Id);
            return false;
        }
        finally
        {
            // CancellationToken.None: the outcome must still be written even if shutdown was what
            // interrupted the run above — otherwise the row is left stuck at Succeeded == null forever.
            run.CompletedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Keeps only the <see cref="MaxRetainedDatasets"/> most recently created <see cref="DmrDataset"/>
    /// rows; anything older is deleted, both the row and its backing SQLite files
    /// (<see cref="DmrDataset.DatabasePath"/> + "-wal"/"-shm"). Called right after a new dataset is
    /// registered, so it only ever needs to prune at most one dataset per run in normal operation.
    /// </summary>
    private async Task CleanupOldDatasetsAsync(AppIdentityDbContext dbContext, CancellationToken cancellationToken)
    {
        var staleDatasets = await dbContext.DmrDatasets
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip(MaxRetainedDatasets)
            .ToListAsync(cancellationToken);

        if (staleDatasets.Count == 0)
            return;

        foreach (var dataset in staleDatasets)
        {
            DeleteDatabaseFiles(dataset.DatabasePath);
            dbContext.DmrDatasets.Remove(dataset);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DmrWorker: pruned {Count} dataset(s) beyond the {MaxRetained} most recently retained.", staleDatasets.Count, MaxRetainedDatasets);
    }

    /// <summary>Best-effort delete of a dataset's SQLite files. Logged and swallowed rather than failing
    /// the run — a leftover file on disk is a cheap problem, but re-ingesting a whole export because
    /// cleanup couldn't remove an old .db file would not be.</summary>
    private void DeleteDatabaseFiles(string databasePath)
    {
        foreach (var path in new[] { databasePath, databasePath + "-wal", databasePath + "-shm" })
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DmrWorker: failed to delete pruned dataset file {Path}.", path);
            }
        }
    }

    /// <summary>Empties ".\app_files\downloads" and ".\app_files\temp" (the fixed directories
    /// <see cref="IFtpService"/>/<see cref="ZipExtensions"/> always use) once a dataset has been
    /// registered, so the next run starts from a clean slate. Best-effort per directory — logged and
    /// swallowed rather than failing an otherwise-successful run.</summary>
    private void ClearDownloadAndTempDirectories(IHostEnvironment environment)
    {
        ClearDirectory(Path.Combine(environment.ContentRootPath, "app_files", "downloads"));
        ClearDirectory(Path.Combine(environment.ContentRootPath, "app_files", "temp"));
    }

    private void ClearDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        try
        {
            foreach (var file in Directory.GetFiles(path))
                File.Delete(file);
            foreach (var directory in Directory.GetDirectories(path))
                Directory.Delete(directory, recursive: true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DmrWorker: failed to fully clear {Path}.", path);
        }
    }
}

using System.Diagnostics;
using System.Reflection;
using DMR.Data;
using DMR.Data.Models;
using DMR.Services.Health.Dtos;
using DMR.Services.Health.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DMR.Services.Health;

/// <inheritdoc cref="IHealthService"/>
public class HealthService : IHealthService
{
    /// <summary>Below this many days old, the newest DMR dataset's <see cref="DmrDataset.SourceFileModifiedUtc"/>
    /// is considered fresh — comfortably inside Motorstyrelsen's weekly export cadence, so no warning is
    /// raised at all.</summary>
    private const int WarningThresholdDays = 8;

    /// <summary>At or above this many days old, the newest dataset is considered stale rather than merely
    /// "the weekly update hasn't landed yet" — two missed weekly updates in a row, which points at an actual
    /// problem (the FTP feed, or <c>DmrWorker</c> itself) rather than normal update timing.</summary>
    private const int StaleThresholdDays = 14;

    /// <summary>Process start time, used for <see cref="HealthCheckResponseDto.UptimeSeconds"/>. Read once
    /// (it never changes for the life of the process) rather than on every check.</summary>
    private static readonly DateTime ProcessStartedAtUtc = Process.GetCurrentProcess().StartTime.ToUniversalTime();

    /// <summary>Assembly version of the running build, resolved once. Null if it couldn't be read (no
    /// version metadata embedded in the build).</summary>
    private static readonly string? AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();

    private readonly AppIdentityDbContext _dbContext;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<HealthService> _logger;

    public HealthService(AppIdentityDbContext dbContext, IHostEnvironment environment, ILogger<HealthService> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _logger = logger;
    }

    public async Task<HealthCheckResponseDto> CheckAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new HealthCheckResponseDto
        {
            TimestampUtc = DateTime.UtcNow,
            Environment = _environment.EnvironmentName,
            Version = AssemblyVersion,
            UptimeSeconds = (DateTime.UtcNow - ProcessStartedAtUtc).TotalSeconds
        };

        try
        {
            // Same "current dataset" lookup as DmrService.LookupVehicleAsync (ORDER BY CreatedAtUtc DESC,
            // first) — only reads DmrDataset's own metadata row in AppIdentityDbContext, never opens the
            // dataset's actual dmr_<date>.db file, so this stays fast enough to poll frequently.
            var latestDataset = await _dbContext.DmrDatasets
                .OrderByDescending(d => d.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            response.DmrDataFreshness = await BuildDmrDataFreshnessAsync(latestDataset, cancellationToken);
            response.Status = response.DmrDataFreshness.Status;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "HealthService: health check failed while reading DMR dataset freshness.");
            response.Status = HealthStatus.Unhealthy;
            response.ErrorMessage = ex.Message;
        }

        stopwatch.Stop();
        response.ResponseTimeMs = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 1);
        return response;
    }

    /// <summary>Judges <paramref name="latestDataset"/>'s age against <see cref="WarningThresholdDays"/>/
    /// <see cref="StaleThresholdDays"/> — see <see cref="DmrDataFreshnessDto"/> for the exact thresholds and
    /// messages. Logs (so it shows up in <c>LogController</c>'s searchable history) whenever the result isn't
    /// <see cref="HealthStatus.Healthy"/>.</summary>
    private async Task<DmrDataFreshnessDto> BuildDmrDataFreshnessAsync(DmrDataset? latestDataset, CancellationToken cancellationToken)
    {
        if (latestDataset is null)
        {
            // Distinguishes "nothing has ever been ingested, and nothing is happening" (a real problem) from
            // "this is a fresh deploy and the first, often multi-hour, download/conversion is still running"
            // (expected, not a problem). DmrWorker only inserts a DmrDataset row once ConvertAsync has fully
            // succeeded (see DmrWorker.RunOnceAsync), so an in-progress first run leaves DmrDatasets empty
            // for as long as it runs — this is the only way to tell the two apart.
            var hasActiveRun = await _dbContext.DmrWorkerRuns
                .AnyAsync(r => r.CompletedAtUtc == null, cancellationToken);

            if (hasActiveRun)
            {
                const string inProgressMessage = "Henter data fra Motorregisteret for første gang.";
                _logger.LogInformation("HealthService: {Message}", inProgressMessage);
                return new DmrDataFreshnessDto
                {
                    Status = HealthStatus.Warning,
                    Message = inProgressMessage,
                    IngestInProgress = true
                };
            }

            // No dataset has ever been ingested, and nothing is currently running either — at least as
            // stale as the 14+ day case, so it gets the same Unhealthy severity, but with its own message
            // since "Forældet data." wouldn't make sense when there was never any data to begin with.
            const string noDatasetMessage = "Der er endnu ikke modtaget data fra Motorregisteret.";
            _logger.LogWarning("HealthService: {Message}", noDatasetMessage);
            return new DmrDataFreshnessDto { Status = HealthStatus.Unhealthy, Message = noDatasetMessage };
        }

        var ageInDays = (DateTime.UtcNow - latestDataset.SourceFileModifiedUtc).TotalDays;
        var (status, message) = ageInDays switch
        {
            >= StaleThresholdDays => (HealthStatus.Unhealthy, "Forældet data."),
            >= WarningThresholdDays => (HealthStatus.Warning, "Afventer ny data fra Motorregisteret."),
            _ => (HealthStatus.Healthy, (string?)null)
        };

        if (message is not null)
        {
            _logger.LogWarning(
                "HealthService: {Message} Nyeste DMR-datasæt ({SourceFileName}) er {AgeInDays:0.0} dage gammelt (kildefil modtaget {SourceFileModifiedUtc:yyyy-MM-dd} UTC).",
                message, latestDataset.SourceFileName, ageInDays, latestDataset.SourceFileModifiedUtc);
        }

        return new DmrDataFreshnessDto
        {
            SourceFileName = latestDataset.SourceFileName,
            SourceFileModifiedUtc = latestDataset.SourceFileModifiedUtc,
            DatasetCreatedAtUtc = latestDataset.CreatedAtUtc,
            AgeInDays = Math.Round(ageInDays, 1),
            Status = status,
            Message = message
        };
    }
}

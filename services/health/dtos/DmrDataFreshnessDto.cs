namespace DMR.Services.Health.Dtos;

/// <summary>How old the newest ingested <see cref="Data.Models.DmrDataset"/> is, judged against
/// Motorstyrelsen's weekly export cadence (see <c>bgServices/DmrWorker.cs</c>) — the extra validation
/// parameter on top of the plain keepalive check. Exception to the request/response pairing rule: a small
/// supporting item type nested inside <see cref="HealthCheckResponseDto"/>, never exchanged on its own.
///
/// <see cref="Status"/>/<see cref="Message"/> follow fixed day thresholds (see
/// <c>HealthService.WarningThresholdDays</c>/<c>StaleThresholdDays</c>) measured against
/// <see cref="SourceFileModifiedUtc"/> — the "Modified" date the FTP server reported for the source XML
/// export, exactly as stored on <see cref="Data.Models.DmrDataset.SourceFileModifiedUtc"/> — not against
/// <see cref="DatasetCreatedAtUtc"/> (when this app happened to finish ingesting it):
/// <list type="bullet">
/// <item>&lt; 8 days old: <see cref="HealthStatus.Healthy"/>, no message.</item>
/// <item>8-13 days old: <see cref="HealthStatus.Warning"/>, "Afventer ny data fra Motorregisteret." — the
/// weekly update hasn't shown up yet, but there's no reason to worry yet either.</item>
/// <item>14+ days old: <see cref="HealthStatus.Unhealthy"/>, "Forældet data." — two missed weekly updates
/// in a row means something is actually wrong (the FTP feed, or <c>DmrWorker</c> itself).</item>
/// <item>No dataset ingested at all yet, and no <see cref="Data.Models.DmrWorkerRun"/> currently in
/// progress: <see cref="HealthStatus.Unhealthy"/>, "Der er endnu ikke modtaget data fra Motorregisteret." —
/// nothing has ever been ingested and nothing is happening, a real problem.</item>
/// <item>No dataset ingested at all yet, but a <see cref="Data.Models.DmrWorkerRun"/> <em>is</em> currently
/// in progress (see <see cref="IngestInProgress"/>): <see cref="HealthStatus.Warning"/>, "Henter data fra
/// Motorregisteret for første gang." — a fresh deploy's first, often multi-hour, download/conversion is
/// still running; expected, not a problem.</item>
/// </list>
/// Whenever <see cref="Message"/> is set, <c>HealthService</c> also logs it (see services/health/docs), so
/// it shows up in <c>LogController</c>'s searchable log history, not just in this one response — as a
/// warning for the two problem cases above, as information for the "first ingest in progress" case.</summary>
public class DmrDataFreshnessDto
{
    /// <summary>Name of the source export file the newest dataset was built from, e.g.
    /// "ESStatistikListeModtag-20260906-175613.zip". Null if no dataset has been ingested yet.</summary>
    public string? SourceFileName { get; set; }

    /// <summary>The "Modified" date the FTP server reported for <see cref="SourceFileName"/> — what
    /// <see cref="AgeInDays"/> is measured against. Null if no dataset has been ingested yet.</summary>
    public DateTime? SourceFileModifiedUtc { get; set; }

    /// <summary>When this dataset was registered (finished being converted), for reference only — not what
    /// the freshness check is measured against. Null if no dataset has been ingested yet.</summary>
    public DateTime? DatasetCreatedAtUtc { get; set; }

    /// <summary>Age of <see cref="SourceFileModifiedUtc"/> relative to now, in days, rounded to one decimal.
    /// Null if no dataset has been ingested yet.</summary>
    public double? AgeInDays { get; set; }

    public HealthStatus Status { get; set; }

    /// <summary>Set only when <see cref="Status"/> is <see cref="HealthStatus.Warning"/> or
    /// <see cref="HealthStatus.Unhealthy"/> — see the threshold list above. Null when the data is fresh.</summary>
    public string? Message { get; set; }

    /// <summary>True only when no dataset has been ingested yet <em>and</em> a <see cref="Data.Models.DmrWorkerRun"/>
    /// is currently mid-run (<c>CompletedAtUtc == null</c>) — see the threshold list above. Always false once
    /// at least one dataset exists, even if <c>DmrWorker</c> happens to be mid-run on a newer one; this field
    /// only exists to tell "first ingest ever, still running" apart from "nothing has ever run". Note this
    /// can't distinguish a genuinely running first ingest from a crashed one that left its
    /// <see cref="Data.Models.DmrWorkerRun"/> row stuck at <c>CompletedAtUtc == null</c> forever (see that
    /// type's own doc comment) — a first ingest stuck at <c>Warning</c> for implausibly long is worth checking
    /// via <c>LogController</c>.</summary>
    public bool IngestInProgress { get; set; }
}

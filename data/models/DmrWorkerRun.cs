namespace DMR.Data.Models;

/// <summary>
/// One row per run of <c>DmrWorker</c> (see bgServices/DmrWorker.cs) — a log of when the daily DMR ingest
/// fired and how it went. Written when a run starts and updated when it finishes, so a run still in
/// progress shows <see cref="CompletedAtUtc"/> == null, and a crash leaves that row visibly stuck instead
/// of the run silently vanishing.
/// </summary>
public class DmrWorkerRun
{
    /// <summary>Primary key (identity column).</summary>
    public int Id { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Null while the run is still in progress.</summary>
    public DateTime? CompletedAtUtc { get; set; }

    /// <summary>Null while in progress; true/false once <see cref="CompletedAtUtc"/> is set.</summary>
    public bool? Succeeded { get; set; }

    /// <summary>Exception message if the run failed; null otherwise.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Informational outcome message for a run that succeeded without an error — e.g. "Database
    /// is up to date." when the newest FTP file was already ingested by an earlier run. Null for a run
    /// that downloaded/converted a new file, or that failed (see <see cref="ErrorMessage"/> instead).</summary>
    public string? Message { get; set; }

    /// <summary>Name of the newest file seen on the FTP server during this run (before download/skip
    /// decisions), e.g. "ESStatistikListeModtag-20260906-175613.zip". Null if the FTP listing itself
    /// failed or returned no files.</summary>
    public string? SourceFileName { get; set; }

    /// <summary>Last-modified timestamp (UTC) the FTP server reported for <see cref="SourceFileName"/>.
    /// This is what the next run compares the newest listed file against (see <see cref="DmrDataset.SourceFileModifiedUtc"/>)
    /// to decide whether that file was already ingested.</summary>
    public DateTime? SourceFileModifiedUtc { get; set; }
}

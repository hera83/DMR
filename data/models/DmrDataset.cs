namespace DMR.Data.Models;

/// <summary>
/// One row per DMR export successfully converted to a SQLite database by <c>DmrWorker</c> (see
/// bgServices/DmrWorker.cs and <see cref="Services.Dmr.Interfaces.IDmrService.ConvertAsync"/>). A row is
/// only ever inserted after conversion has already succeeded, so a row's mere existence means "active and
/// ready" — there is no separate in-progress state to model here (that lives on <see cref="DmrWorkerRun"/>
/// instead).
///
/// <see cref="SourceFileName"/>/<see cref="SourceFileModifiedUtc"/> double as the worker's "did the FTP
/// server's newest file change since last time?" check: before downloading, the worker compares the newest
/// listed file's name/modified date against the newest row here, and skips straight to a "database is up
/// to date" success if they match.
///
/// A future <c>DmrController</c> is expected to read whichever row has the latest <see cref="CreatedAtUtc"/>
/// among <see cref="IsActive"/> rows. Only the <see cref="DmrWorker.MaxRetainedDatasets"/> most recent rows
/// are ever kept — see <c>DmrWorker.CleanupOldDatasetsAsync</c>, which deletes both the row and its backing
/// <see cref="DatabasePath"/> file (+ "-wal"/"-shm") once a new dataset pushes it past that limit.
/// </summary>
public class DmrDataset
{
    /// <summary>Primary key (identity column).</summary>
    public int Id { get; set; }

    /// <summary>Name of the FTP export file this dataset was built from, e.g.
    /// "ESStatistikListeModtag-20260906-175613.zip".</summary>
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>Last-modified timestamp (UTC) the FTP server reported for <see cref="SourceFileName"/> at
    /// the time it was downloaded.</summary>
    public DateTime SourceFileModifiedUtc { get; set; }

    /// <summary>Full local path of the SQLite database this dataset was converted into
    /// (".\app_dbs\dmr_&lt;yyyy-MM-dd&gt;.db" — see <see cref="Services.Dmr.Dtos.DmrConvertResponseDto.DatabasePath"/>).</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>When this dataset was registered, immediately after <c>IDmrService.ConvertAsync</c>
    /// succeeded.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>True for every dataset that has been registered and not yet pruned by the retention
    /// cleanup. Always true today (rows falling out of retention are deleted outright rather than flipped
    /// to false) — kept as an explicit column so a future soft-retire policy doesn't need a schema change.</summary>
    public bool IsActive { get; set; } = true;
}

using DMR.Services.Dmr.Dtos;

namespace DMR.Services.Dmr.Interfaces;

/// <summary>
/// Converts the unpacked Motorstyrelsen DMR "ESStatistikListeModtag" vehicle export (a multi-GB XML file,
/// see <see cref="Extensions.ZipExtensions"/>) into a local SQLite database, so its contents can be looked
/// up without re-parsing the XML. See services/dmr/docs.
/// </summary>
public interface IDmrService
{
    /// <summary>
    /// Streams the source XML file one &lt;Statistik&gt; (vehicle) record at a time and writes it into the
    /// SQLite database, rebuilding that day's dated database file from scratch. This can take a long time for the full,
    /// multi-GB export (millions of vehicle records) — run it from a background job, never awaited inside
    /// an HTTP request (see services/ftp/docs for why, the same reasoning applies here).
    /// </summary>
    Task<DmrConvertResponseDto> ConvertAsync(DmrConvertRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a single vehicle by registration number in the newest registered
    /// <see cref="Data.Models.DmrDataset"/> (see <c>bgServices/DmrWorker.cs</c>). Fast — a single indexed
    /// SQLite query plus a handful of small child-table queries, safe to await directly inside an HTTP
    /// request (unlike <see cref="ConvertAsync"/>).
    /// </summary>
    Task<DmrLookupVehicleResponseDto> LookupVehicleAsync(DmrLookupVehicleRequestDto request, CancellationToken cancellationToken = default);
}

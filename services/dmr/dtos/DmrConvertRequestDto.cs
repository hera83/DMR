namespace DMR.Services.Dmr.Dtos;

/// <summary>Request for <see cref="Interfaces.IDmrService.ConvertAsync"/>. The source XML path and the
/// destination SQLite database path are both fixed (".\app_files\temp\ESStatistikListeModtag.xml" and
/// ".\app_dbs\dmr_&lt;yyyy-MM-dd&gt;.db", date-stamped with today's date — see services/dmr/docs), so
/// neither is a field here: callers can't redirect the conversion to arbitrary files.</summary>
public class DmrConvertRequestDto
{
    /// <summary>Optional cap on how many &lt;Statistik&gt; vehicle records to import, for a quick smoke
    /// test on a subset instead of the full multi-hour run. Null imports every record in the file.</summary>
    public int? MaxRecords { get; set; }
}

namespace DMR.Services.Dmr.Dtos;

/// <summary>Response from <see cref="Interfaces.IDmrService.ConvertAsync"/>.</summary>
public class DmrConvertResponseDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Number of &lt;Statistik&gt; vehicle records written to the database.</summary>
    public int RecordsImported { get; set; }

    /// <summary>Number of &lt;Statistik&gt; records skipped — either missing a KoeretoejIdent, or failing
    /// to deserialize (an unanticipated field shape elsewhere in the export; see services/dmr/docs "Known
    /// gaps"). A skip never aborts the rest of the import.</summary>
    public int RecordsSkipped { get; set; }

    /// <summary>Full local path of the SQLite database that was (re)built.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    public TimeSpan Duration { get; set; }
}

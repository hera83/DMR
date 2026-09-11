using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:MaaleNormStruktur&gt; — the measurement standard (e.g. "WLTP") a fuel/power source's
/// consumption and emission figures were measured under. Nested inside <see cref="DrivmiddelStrukturXml"/>,
/// per fuel/power source (not per vehicle) — note the tag has no "Koeretoej" prefix unlike its siblings.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class MaaleNormStrukturXml
{
    [XmlElement("KoeretoejMotorMaaleNormTypeNummer")]
    public long? Nummer { get; set; }

    [XmlElement("KoeretoejMotorMaaleNormTypeNavn")]
    public string? Navn { get; set; }
}

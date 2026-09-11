using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejBraendstofStruktur&gt; — fuel consumption and CO2 figures for one fuel/power
/// source, nested inside <see cref="DrivmiddelStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejBraendstofStrukturXml
{
    // double, not decimal — see the note on KoeretoejMiljoeOplysningStrukturXml.Partikler: the export can
    // format small values in scientific notation (e.g. "4.7E-4"), which XmlSerializer's decimal reader
    // rejects but its double reader accepts.
    [XmlElement("KoeretoejMotorKmPerLiter")]
    public double? KmPerLiter { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningCO2Udslip")]
    public double? CO2Udslip { get; set; }
}

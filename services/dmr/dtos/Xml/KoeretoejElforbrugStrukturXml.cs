using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejElforbrugStruktur&gt; — electric power consumption for one fuel/power
/// source (present for electric/hybrid drivetrains), nested inside <see cref="DrivmiddelStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejElforbrugStrukturXml
{
    // double, not decimal — see the note on KoeretoejMiljoeOplysningStrukturXml.Partikler.
    [XmlElement("KoeretoejMotorElektriskForbrug")]
    public double? ElektriskForbrug { get; set; }
}

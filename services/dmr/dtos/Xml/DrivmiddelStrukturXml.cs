using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:DrivmiddelStruktur&gt; — one fuel/power source of a vehicle (up to 2 observed, e.g.
/// petrol + electric for a hybrid), nested inside <see cref="KoeretoejDrivmiddelSamlingItemXml"/>. Written
/// to the <c>KoeretoejDrivmiddel</c> child table — see services/dmr/docs.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class DrivmiddelStrukturXml
{
    [XmlElement("MaaleNormStruktur")]
    public MaaleNormStrukturXml? MaaleNormStruktur { get; set; }

    [XmlElement("DrivkraftTypeStruktur")]
    public DrivkraftTypeStrukturXml? DrivkraftTypeStruktur { get; set; }

    [XmlElement("KoeretoejBraendstofStruktur")]
    public KoeretoejBraendstofStrukturXml? KoeretoejBraendstofStruktur { get; set; }

    [XmlElement("KoeretoejElforbrugStruktur")]
    public KoeretoejElforbrugStrukturXml? KoeretoejElforbrugStruktur { get; set; }

    [XmlElement("KoeretoejMotorDrivmiddelPrimaer")]
    public bool? DrivmiddelPrimaer { get; set; }
}

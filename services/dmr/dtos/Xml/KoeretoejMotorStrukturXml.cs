using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejMotorStruktur&gt;, nested inside <see cref="KoeretoejOplysningGrundStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejMotorStrukturXml
{
    [XmlElement("KoeretoejMotorCylinderAntal")]
    public int? CylinderAntal { get; set; }

    // double, not decimal — see the note on KoeretoejMiljoeOplysningStrukturXml.Partikler.
    [XmlElement("KoeretoejMotorSlagVolumen")]
    public double? SlagVolumen { get; set; }

    [XmlElement("KoeretoejMotorSlagVolumenIkkeTilgaengelig")]
    public bool? SlagVolumenIkkeTilgaengelig { get; set; }

    [XmlElement("KoeretoejMotorStoersteEffekt")]
    public double? StoersteEffekt { get; set; }

    [XmlElement("KoeretoejMotorStoersteEffektIkkeTilgaengelig")]
    public bool? StoersteEffektIkkeTilgaengelig { get; set; }

    [XmlElement("KoeretoejMotorKilometerstand")]
    public int? Kilometerstand { get; set; }

    [XmlElement("KoeretoejMotorKilometerstandDokumentation")]
    public bool? KilometerstandDokumentation { get; set; }

    [XmlElement("KoeretoejMotorKilometerstandIkkeTilgaengelig")]
    public bool? KilometerstandIkkeTilgaengelig { get; set; }

    [XmlElement("KoeretoejMotorInnovativTeknik")]
    public bool? InnovativTeknik { get; set; }

    // double, not int, despite the "Antal" (count) name — confirmed against the real export (e.g. "0.8"):
    // this is the CO2 savings (g/km) from the innovative technology flagged by InnovativTeknik, per EU
    // vehicle type-approval "eco-innovation" reporting, not a count of anything. double rather than decimal
    // for the same reason as every other field here — see the note on
    // KoeretoejMiljoeOplysningStrukturXml.Partikler.
    [XmlElement("KoeretoejMotorInnovativTeknikAntal")]
    public double? InnovativTeknikAntal { get; set; }

    [XmlElement("KoeretoejMotorKoerselStoej")]
    public double? KoerselStoej { get; set; }

    [XmlElement("KoeretoejMotorStandStoej")]
    public double? StandStoej { get; set; }

    [XmlElement("KoeretoejMotorStandStoejOmdrejningstal")]
    public int? StandStoejOmdrejningstal { get; set; }

    [XmlElement("KoeretoejMotorBraendselscelle")]
    public bool? Braendselscelle { get; set; }

    [XmlElement("KoeretoejMotorMaerkning")]
    public string? Maerkning { get; set; }

    [XmlElement("KoeretoejDrivmiddelSamlingStruktur")]
    public KoeretoejDrivmiddelSamlingStrukturXml? KoeretoejDrivmiddelSamlingStruktur { get; set; }
}

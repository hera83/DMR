using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejMiljoeOplysningStruktur&gt; — environmental/emission figures, nested inside
/// <see cref="KoeretoejOplysningGrundStrukturXml"/>. All fields are flat direct children (confirmed against
/// the source sample), no further nesting.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejMiljoeOplysningStrukturXml
{
    // All of this struct's measured fields are double, not decimal. Confirmed against the real export:
    // Partikler ("particulates", typically a very small g/km figure) showed up in scientific notation —
    // e.g. "4.7E-4" — which XmlSerializer's generated decimal reader (System.Decimal.Parse /
    // XmlConvert.ToDecimal) rejects, because xs:decimal's lexical space doesn't permit an exponent; its
    // double reader (XmlConvert.ToDouble) does. Every other small-measurement field on this struct carries
    // the same risk even though only Partikler has actually been seen doing it yet, so all of them were
    // widened together rather than waiting to find each one the hard way (same reasoning as
    // KoeretoejOplysningTraekkendeAksler — see "Per-record resilience" in services/dmr/docs). double loses
    // nothing meaningful here: these are physical measurements, not exact-decimal financial values, and the
    // destination SQLite column type is REAL (an 8-byte float) either way.
    [XmlElement("KoeretoejMiljoeOplysningEmissionCO")]
    public double? EmissionCO { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningEmissionHCPlusNOX")]
    public double? EmissionHCPlusNOX { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningEmissionNOX")]
    public double? EmissionNOX { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningNyttelastvaerdi")]
    public double? Nyttelastvaerdi { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningPartikelFilter")]
    public bool? PartikelFilter { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningPartikler")]
    public double? Partikler { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningRoegtaethed")]
    public double? Roegtaethed { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningRoegtaethedOmdrejningstal")]
    public int? RoegtaethedOmdrejningstal { get; set; }

    [XmlElement("KoeretoejMiljoeOplysningTungtNulEmissionKoeretoej")]
    public bool? TungtNulEmissionKoeretoej { get; set; }
}

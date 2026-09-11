using System.Xml.Serialization;

namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>Maps &lt;ns:KoeretoejBetegnelseStruktur&gt; — brand, model, variant and type, nested inside
/// <see cref="KoeretoejOplysningGrundStrukturXml"/>.</summary>
[XmlType(Namespace = DmrXmlNamespace.Value)]
public sealed class KoeretoejBetegnelseStrukturXml
{
    [XmlElement("KoeretoejMaerkeTypeNummer")]
    public long? MaerkeTypeNummer { get; set; }

    [XmlElement("KoeretoejMaerkeTypeNavn")]
    public string? MaerkeTypeNavn { get; set; }

    [XmlElement("Model")]
    public ModelXml? Model { get; set; }

    [XmlElement("Variant")]
    public VariantXml? Variant { get; set; }

    [XmlElement("Type")]
    public TypeXml? Type { get; set; }
}

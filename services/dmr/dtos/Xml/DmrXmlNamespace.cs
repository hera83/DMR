namespace DMR.Services.Dmr.Dtos.Xml;

/// <summary>
/// The single XML namespace used throughout Motorstyrelsen's "ESStatistikListeModtag" DMR export (see
/// services/dmr/docs). Every [XmlRoot]/[XmlType]/[XmlElement] attribute under this folder references
/// <see cref="Value"/> so the whole set stays in sync if the export's namespace ever changes.
/// Exception to the one-class-per-file DTO pairing rule: a small supporting constant, never exchanged
/// on its own.
/// </summary>
internal static class DmrXmlNamespace
{
    public const string Value = "http://skat.dk/dmr/2007/05/31/";
}

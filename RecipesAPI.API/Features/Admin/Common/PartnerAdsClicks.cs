using System.Globalization;
using System.Xml.Serialization;

namespace RecipesAPI.API.Features.Admin.Common;

[XmlRoot("klikoversigt")]
public class PartnerAdsClickSummary
{
    [XmlElement("klik")]
    public List<PartnerAdsClick> Clicks { get; set; } = new List<PartnerAdsClick>();
}

[XmlRoot("klik")]
public class PartnerAdsClick
{
    [XmlElement("programid")]
    public string ProgramId { get; set; } = "";
    [XmlElement("programnavn")]
    public string ProgramName { get; set; } = "";

    [XmlElement("dato")]
    public string DateStr { get; set; } = "";
    [XmlElement("tid")]
    public string TimeStr { get; set; } = "";
    public DateTime? Timestamp => DateTime.TryParseExact($"{DateStr} {TimeStr}", "d-M-yyyy HH:mm", CultureInfo.GetCultureInfo("da-dk"), System.Globalization.DateTimeStyles.None, out var parsed) ? parsed : null;

    [XmlElement("url")]
    public string? Url { get; set; }

    [XmlElement("salg")]
    public string SaleStr { get; set; } = "";
    public bool Sale => string.Equals("nej", SaleStr, StringComparison.OrdinalIgnoreCase) ? false : true;

}
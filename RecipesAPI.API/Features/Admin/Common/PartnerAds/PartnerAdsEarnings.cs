using System.Xml.Serialization;

namespace RecipesAPI.API.Features.Admin.Common;

[XmlRoot("intervalindtjening")]
public class PartnerAdsEarningsInterval
{
    [XmlElement("indtjening")]
    public PartnerAdsEarning Earning { get; set; } = new PartnerAdsEarning();
}

[XmlRoot("indtjening")]
public class PartnerAdsEarning
{
    [XmlElement("sum")]
    public string SumStr { get; set; } = "";
    public double Sum => double.TryParse(SumStr.Replace(",", "."), out var _parsed) ? _parsed : 0;
}
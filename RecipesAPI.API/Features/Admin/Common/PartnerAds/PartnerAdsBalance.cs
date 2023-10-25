using System.Xml.Serialization;

namespace RecipesAPI.API.Features.Admin.Common;

[XmlRoot("saldopost")]
public class PartnerAdsBalanceItem
{
    [XmlElement("type")]
    public string Type { get; set; } = "";
    [XmlElement("beloeb")]
    public string AmountStr { get; set; } = "";

    public double Amount => double.TryParse(AmountStr.Replace(",", "."), out var _parsedAmount) ? _parsedAmount : 0;
}

[XmlRoot("saldooplysninger")]
public class PartnerAdsBalance
{
    [XmlElement("saldopost")]
    public List<PartnerAdsBalanceItem> Items { get; set; } = new List<PartnerAdsBalanceItem>();
}
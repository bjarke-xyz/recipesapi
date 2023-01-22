using System.Xml.Serialization;

namespace RecipesAPI.API.Features.Admin.Common;

[XmlRoot("program")]
public class PartnerAdsProgram
{
    [XmlElement(ElementName = "programid")]
    public string ProgramId { get; set; } = "";

    [XmlElement(ElementName = "programnavn")]
    public string ProgramName { get; set; } = "";

    [XmlElement(ElementName = "programurl")]
    public string ProgramUrl { get; set; } = "";

    [XmlElement(ElementName = "programbeskrivelse")]
    public string ProgramDescription { get; set; } = "";

    [XmlElement(ElementName = "kategoriid")]
    public string CategoryId { get; set; } = "";

    [XmlElement(ElementName = "kategorinavn")]
    public string CategoryName { get; set; } = "";

    [XmlElement(ElementName = "underkategori")]
    public string SubCategory { get; set; } = "";


    [XmlElement(ElementName = "kliksats")]
    public double ClickRate { get; set; }

    [XmlElement(ElementName = "leadsats")]
    public double LeadRate { get; set; }

    [XmlElement(ElementName = "provision")]
    public double Provision { get; set; }

    [XmlElement("feedcur")]
    public string Currency { get; set; } = "";

    [XmlElement("feedmarket")]
    public string Market { get; set; } = "";
}

[XmlRoot("partnerprogrammer")]
public class PartnerAdsPrograms
{
    [XmlElement(ElementName = "program")]
    public List<PartnerAdsProgram> Programs { get; set; } = new List<PartnerAdsProgram>();

}
using System.Xml.Serialization;

namespace RecipesAPI.API.Features.Admin.Common;

[XmlRoot("program")]
public class PartnerAdsProgramStat
{
    [XmlElement("programid")]
    public string ProgramId { get; set; } = "";

    [XmlElement("programnavn")]
    public string ProgramName { get; set; } = "";

    [XmlElement("klik")]
    public int Clicks { get; set; }

    [XmlElement("leads")]
    public int Leads { get; set; }

    [XmlElement("leadbelob")]
    public double LeadAmount { get; set; }

    [XmlElement("salg")]
    public int Sales { get; set; }

    [XmlElement("ordreoms")]
    public double OrderRevenue { get; set; }

    [XmlElement("salgbelob")]
    public double SalesAmount { get; set; }

    [XmlElement("programtotal")]
    public double ProgramTotal { get; set; }
}

[XmlRoot("programstat")]
public class PartnerAdsProgramStats
{
    [XmlElement("program")]
    public List<PartnerAdsProgramStat> Programs { get; set; } = new List<PartnerAdsProgramStat>();
}
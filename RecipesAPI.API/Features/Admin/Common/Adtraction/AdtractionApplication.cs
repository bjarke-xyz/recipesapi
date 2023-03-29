namespace RecipesAPI.API.Features.Admin.Common.Adtraction;

public class AdtractionApplication
{
    public int ChannelId { get; set; }
    public string ChannelName { get; set; } = "";
    public int Status { get; set; }
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = "";
}
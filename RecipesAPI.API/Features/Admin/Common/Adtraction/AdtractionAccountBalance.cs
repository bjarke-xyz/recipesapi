namespace RecipesAPI.API.Features.Admin.Common.Adtraction;

public class AdtractionAccountBalance
{
    public double PendingBalance { get; set; }
    public double ConfirmedBalance { get; set; }
    public double InvoicedBalance { get; set; }
    public double PayableBalance { get; set; }
    public double TotalBalance { get; set; }
}
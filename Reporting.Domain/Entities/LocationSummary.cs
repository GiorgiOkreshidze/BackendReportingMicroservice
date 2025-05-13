namespace Reporting.Domain.Entities;

public class LocationSummary
{
    public string LocationId { get; set; }
    public string LocationName { get; set; }
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public int CurrentOrdersCount { get; set; }
    public int PreviousOrdersCount { get; set; }
    public double DeltaOrdersPercent { get; set; }
    public double CurrentAvgCuisineFeedback { get; set; }
    public int CurrentMinCuisineFeedback { get; set; }
    public double PreviousAvgCuisineFeedback { get; set; }
    public double DeltaAvgCuisinePercent { get; set; }
    public decimal CurrentRevenue { get; set; }
    public decimal PreviousRevenue { get; set; }
    public decimal DeltaRevenuePercent { get; set; }
}
using Reporting.Application.Interfaces;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Repositories;
using Reporting.Infrastructure.Repositories.Interfaces;

namespace Reporting.Application;

public class ReportProcessService : IReportProcessService
{
    public async Task<(List<SummaryEntry> WaiterSummaries, List<LocationSummary> LocationSummaries)> ProcessReports(
        List<Report> currentWeek, 
        List<Report> previousWeek, 
        DateTime startDate,
        DateTime endDate)
    {
        // Process waiter summaries
        var waiterSummaries = ProcessWaiterSummaries(currentWeek, previousWeek, startDate, endDate);
        
        // Process location summaries
        var locationSummaries = await ProcessLocationSummariesAsync(currentWeek, previousWeek, startDate, endDate);
        
        return (waiterSummaries, locationSummaries);
    }
    
    private static List<SummaryEntry> ProcessWaiterSummaries(
        List<Report> currentWeek, 
        List<Report> previousWeek, 
        DateTime startDate,
        DateTime endDate)
    {
        var waiterSummaries = new Dictionary<string, SummaryEntry>();
        var currentFeedbackCount = new Dictionary<string, int>();
        var previousFeedbackCount = new Dictionary<string, int>();

        AggregateCurrentWeek(currentWeek, startDate, endDate, waiterSummaries, currentFeedbackCount, previousFeedbackCount);
        AggregatePreviousWeek(previousWeek, startDate, endDate, waiterSummaries, currentFeedbackCount, previousFeedbackCount);

        foreach (var key in waiterSummaries.Keys)
        {
            var entry = waiterSummaries[key];
            CalculateDeltaHours(entry);
            CalculateAverageServiceFeedback(currentFeedbackCount, key, entry, previousFeedbackCount);
            CalculateDeltaServiceFeedback(entry);
            NormalizeMinimumFeedback(entry);
        }
        
        return waiterSummaries.Values.ToList();
    }
    
    private async Task<List<LocationSummary>> ProcessLocationSummariesAsync(
        List<Report> currentWeek,
        List<Report> previousWeek,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var locationSummaries = new Dictionary<string, LocationSummary>();

        // Group reports by location
        var currentByLocation = GroupReportsByLocation(currentWeek);
        var previousByLocation = GroupReportsByLocation(previousWeek);

        // Get all unique location IDs
        var allLocationIds = GetUniqueLocationIds(currentByLocation, previousByLocation);

        foreach (var locationId in allLocationIds)
        {
            var currentLocationReports = currentByLocation.GetValueOrDefault(locationId) ?? new List<Report>();
            var previousLocationReports = previousByLocation.GetValueOrDefault(locationId) ?? new List<Report>();

            // Skip if no data for current period
            if (!currentLocationReports.Any())
                continue;

            // Create location summary
            var locationSummary = CreateInitialLocationSummary(locationId, currentLocationReports, startDate, endDate);

            // Process current period data
            ProcessCurrentPeriodLocationData(locationSummary, currentLocationReports);
            
            // Process previous period data
            ProcessPreviousPeriodLocationData(locationSummary, previousLocationReports);

            // Calculate delta percentages including revenue
            CalculateLocationDeltaPercentages(locationSummary);

            locationSummaries[locationId] = locationSummary;
        }

        return locationSummaries.Values.ToList();
    }
    
    
    private static Dictionary<string, List<Report>> GroupReportsByLocation(List<Report> reports)
    {
        return reports
            .GroupBy(r => r.LocationId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
    
    private static List<string> GetUniqueLocationIds(Dictionary<string, List<Report>> current, Dictionary<string, List<Report>> previous)
    {
        return current.Keys.Union(previous.Keys).ToList();
    }
    
    private LocationSummary CreateInitialLocationSummary(string locationId, List<Report> reports, DateTime startDate, DateTime endDate)
    {
        return new LocationSummary
        {
            LocationId = locationId,
            LocationName = reports.First().Location,
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            CurrentOrdersCount = 0,
            PreviousOrdersCount = 0,
            DeltaOrdersPercent = 0,
            CurrentAvgCuisineFeedback = 0,
            CurrentMinCuisineFeedback = int.MaxValue,
            PreviousAvgCuisineFeedback = 0,
            DeltaAvgCuisinePercent = 0,
            CurrentRevenue = 0,
            PreviousRevenue = 0,
            DeltaRevenuePercent = 0
        };
    }
    
    private static void ProcessCurrentPeriodLocationData(LocationSummary summary, List<Report> reports)
    {
        // Count unique orders
        var currentOrderIds = reports.Select(r => r.OrderId).Distinct().ToList();
        var currentOrderRevenue = reports.Sum(r => r.OrderRevenue);
        summary.CurrentOrdersCount = currentOrderIds.Count;
        summary.CurrentRevenue = currentOrderRevenue;

        // Process cuisine feedback
        int currentFeedbackCount = 0;
        foreach (var report in reports)
        {
            if (report.AverageCuisineFeedback > 0)
            {
                summary.CurrentAvgCuisineFeedback += report.AverageCuisineFeedback;
                currentFeedbackCount++;
            }

            if (report.MinimumCuisineFeedback > 0)
            {
                summary.CurrentMinCuisineFeedback = Math.Min(summary.CurrentMinCuisineFeedback, report.MinimumCuisineFeedback);
            }
        }

        // Normalize feedback values
        if (currentFeedbackCount > 0)
        {
            summary.CurrentAvgCuisineFeedback /= currentFeedbackCount;
        }

        if (summary.CurrentMinCuisineFeedback == int.MaxValue)
        {
            summary.CurrentMinCuisineFeedback = 0;
        }
    }
    
    private static void ProcessPreviousPeriodLocationData(LocationSummary summary, List<Report> reports)
    {
        // Count unique orders
        var previousOrderIds = reports.Select(r => r.OrderId).Distinct().ToList();
        var preciousOrderRevenue = reports.Sum(r => r.OrderRevenue);
        
        summary.PreviousOrdersCount = previousOrderIds.Count;
        summary.PreviousRevenue = preciousOrderRevenue;
        
        // Process cuisine feedback
        int previousFeedbackCount = 0;
        foreach (var report in reports)
        {
            if (report.AverageCuisineFeedback > 0)
            {
                summary.PreviousAvgCuisineFeedback += report.AverageCuisineFeedback;
                previousFeedbackCount++;
            }
        }

        // Normalize feedback values
        if (previousFeedbackCount > 0)
        {
            summary.PreviousAvgCuisineFeedback /= previousFeedbackCount;
        }
    }
    
    private static void CalculateLocationDeltaPercentages(LocationSummary summary)
    {
        // Calculate orders delta percentage
        if (summary.PreviousOrdersCount == 0)
            summary.DeltaOrdersPercent = summary.CurrentOrdersCount > 0 ? 1 : 0;
        else
            summary.DeltaOrdersPercent = (double)(summary.CurrentOrdersCount - summary.PreviousOrdersCount) / 
                                         summary.PreviousOrdersCount;

        // Calculate cuisine feedback delta percentage
        if (summary.PreviousAvgCuisineFeedback == 0)
            summary.DeltaAvgCuisinePercent = summary.CurrentAvgCuisineFeedback > 0 ? 1 : 0;
        else
            summary.DeltaAvgCuisinePercent = (summary.CurrentAvgCuisineFeedback - summary.PreviousAvgCuisineFeedback) /
                                             summary.PreviousAvgCuisineFeedback;
        

        // Calculate revenue delta percentage
        if (summary.PreviousRevenue == 0)
            summary.DeltaRevenuePercent = summary.CurrentRevenue > 0 ? 1 : 0;
        else
            summary.DeltaRevenuePercent = (summary.CurrentRevenue - summary.PreviousRevenue) / 
                                          summary.PreviousRevenue;
    }

    private static void NormalizeMinimumFeedback(SummaryEntry entry)
    {
        // If no feedback was recorded in the current week, set minimum to 0 instead of int.MaxValue
        if (entry.MinimumServiceFeedback == int.MaxValue)
            entry.MinimumServiceFeedback = 0;
    }

    private static void CalculateDeltaServiceFeedback(SummaryEntry entry)
    {
        if (entry.PreviousAverageServiceFeedback == 0)
            entry.DeltaAverageServiceFeedback = entry.CurrentAverageServiceFeedback > 0 ? 1 : 0;
        else
            entry.DeltaAverageServiceFeedback = (entry.CurrentAverageServiceFeedback - entry.PreviousAverageServiceFeedback) 
                                                / entry.PreviousAverageServiceFeedback;
    }

    private static void CalculateAverageServiceFeedback(Dictionary<string, int> currentFeedbackCount, string key, SummaryEntry entry,
        Dictionary<string, int> previousFeedbackCount)
    {
        // Calculate average service feedback for current week
        if (currentFeedbackCount[key] > 0)
            entry.CurrentAverageServiceFeedback /= currentFeedbackCount[key];

        // Calculate average service feedback for previous week
        if (previousFeedbackCount[key] > 0)
            entry.PreviousAverageServiceFeedback /= previousFeedbackCount[key];
    }

    private static void CalculateDeltaHours(SummaryEntry entry)
    {
        if (entry.PreviousHours == 0)
            entry.DeltaHours = entry.CurrentHours > 0 ? 1 : 0; // +100% or 0%
        else
            entry.DeltaHours = ((entry.CurrentHours - entry.PreviousHours) / entry.PreviousHours);
    }

    private static void AggregatePreviousWeek(List<Report> previousWeek, DateTime startDate, DateTime endDate,
        Dictionary<string, SummaryEntry> waiterSummaries, Dictionary<string, int> currentFeedbackCount, Dictionary<string, int> previousFeedbackCount)
    {
        // Aggregate previous week
        foreach (var report in previousWeek)
        {
            var key = $"{report.Waiter}-{report.WaiterEmail}";
            if (!waiterSummaries.ContainsKey(key))
                CreateEmptySummaryEntry(startDate, endDate, waiterSummaries, currentFeedbackCount, previousFeedbackCount, key, report);

            waiterSummaries[key].PreviousHours += report.HoursWorked;
            waiterSummaries[key].PreviousAverageServiceFeedback += report.AverageServiceFeedback;
            if(report.AverageServiceFeedback > 0)
            {
                previousFeedbackCount[key]++;
            }
            
        }
    }

    private static void AggregateCurrentWeek(List<Report> currentWeek, DateTime startDate, DateTime endDate,
        Dictionary<string, SummaryEntry> waiterSummaries, Dictionary<string, int> currentFeedbackCount, Dictionary<string, int> previousFeedbackCount)
    {
        foreach (var report in currentWeek)
        {
            var key = $"{report.Waiter}-{report.WaiterEmail}";
            if (!waiterSummaries.ContainsKey(key))
                CreateEmptySummaryEntry(startDate, endDate, waiterSummaries, currentFeedbackCount, previousFeedbackCount, key, report);

            waiterSummaries[key].CurrentHours += report.HoursWorked;
            waiterSummaries[key].CurrentAverageServiceFeedback += report.AverageServiceFeedback;
            if(report.AverageServiceFeedback > 0)
            {
                currentFeedbackCount[key]++;
            }
            if(report.MinimumServiceFeedback > 0)
            {
                waiterSummaries[key].MinimumServiceFeedback = Math.Min(waiterSummaries[key].MinimumServiceFeedback, report.MinimumServiceFeedback);
            }
        }
    }
    
    private static void CreateEmptySummaryEntry(DateTime startDate, DateTime endDate, Dictionary<string, SummaryEntry> waiterSummaries,
        Dictionary<string, int> currentFeedbackCount, Dictionary<string, int> previousFeedbackCount, string key, Report report)
    {
        waiterSummaries[key] = new SummaryEntry
        {
            Location = report.Location,
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            WaiterName = report.Waiter,
            WaiterEmail = report.WaiterEmail,
            CurrentHours = 0,
            PreviousHours = 0,
            DeltaHours = 0,
            CurrentAverageServiceFeedback = 0,
            MinimumServiceFeedback = int.MaxValue,
            PreviousAverageServiceFeedback = 0,
            DeltaAverageServiceFeedback = 0
        };
                
        currentFeedbackCount[key] = 0;
        previousFeedbackCount[key] = 0;
    }
}
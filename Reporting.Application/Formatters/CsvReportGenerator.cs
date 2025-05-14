using System.Globalization;
using Microsoft.Extensions.Logging;
using Reporting.Application.Formatters.Interfaces;
using Reporting.Application.Formatters.Utils;
using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters;

public class CsvReportGenerator(ILogger<ReportGenerator> logger) : ICsvReportGenerator
{
   public Task<byte[]> GenerateReportBytesAsync(IList<SummaryEntry> statistics)
    {
        string[] headers = {
            "Location", "Start Date", "End Date", "Waiter Name", "Waiter Email",
            "Current Hours", "Previous Hours", "Delta Hours",
            "Current Avg Service Feedback", "Previous Avg Service Feedback",
            "Delta Avg Service Feedback", "Min Service Feedback"
        };

        return GenerateCsvReportAsync(statistics, headers, stat => {
            var deltaHours = ReportFormattingUtils.FormatPercentageDouble(stat.DeltaHours);
            var deltaFeedback = ReportFormattingUtils.FormatPercentageDouble(stat.DeltaAverageServiceFeedback);
            
            return new string[] {
                stat.Location,
                stat.StartDate,
                stat.EndDate,
                stat.WaiterName,
                stat.WaiterEmail,
                stat.CurrentHours.ToString(CultureInfo.InvariantCulture),
                stat.PreviousHours.ToString(CultureInfo.InvariantCulture),
                deltaHours,
                stat.CurrentAverageServiceFeedback.ToString(CultureInfo.InvariantCulture),
                stat.PreviousAverageServiceFeedback.ToString(CultureInfo.InvariantCulture),
                deltaFeedback,
                stat.MinimumServiceFeedback.ToString(CultureInfo.InvariantCulture)
            };
        });
    }

    public Task<byte[]> GenerateReportBytesLocationSummariesAsync(IList<LocationSummary> locationSummaries)
    {
        string[] headers = {
            "Location", "Start Date", "End Date", "Current Orders Count", "Previous Orders Count",
            "Delta Orders %", "Current Avg Cuisine Feedback", "Previous Avg Cuisine Feedback",
            "Delta Avg Cuisine %", "Min Cuisine Feedback",
            "Current Revenue", "Previous Revenue", "Delta Revenue %"
        };
        
        return GenerateCsvReportAsync(locationSummaries, headers, stat => {
            var deltaOrders = ReportFormattingUtils.FormatPercentageDouble(stat.DeltaOrdersPercent);
            var deltaCuisineFeedback = ReportFormattingUtils.FormatPercentageDouble(stat.DeltaAvgCuisinePercent);
            var deltaRevenue = ReportFormattingUtils.FormatPercentageDecimal(stat.DeltaRevenuePercent);
            
            return new string[] {
                stat.LocationName,
                stat.StartDate,
                stat.EndDate,
                stat.CurrentOrdersCount.ToString(CultureInfo.InvariantCulture),
                stat.PreviousOrdersCount.ToString(CultureInfo.InvariantCulture),
                deltaOrders,
                stat.CurrentAvgCuisineFeedback.ToString(CultureInfo.InvariantCulture),
                stat.PreviousAvgCuisineFeedback.ToString(CultureInfo.InvariantCulture),
                deltaCuisineFeedback,
                stat.CurrentMinCuisineFeedback.ToString(CultureInfo.InvariantCulture),
                stat.CurrentRevenue.ToString(CultureInfo.InvariantCulture),
                stat.PreviousRevenue.ToString(CultureInfo.InvariantCulture),
                deltaRevenue
            };
        });
    }

    private Task<byte[]> GenerateCsvReportAsync<T>(
        IList<T> data, 
        string[] headers, 
        Func<T, string[]> createRowValues)
    {
        logger.LogInformation("Generating CSV report for {Count} items", data.Count);

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        
        // Write headers
        writer.WriteLine(string.Join(",", headers.Select(EscapeCsvField)));
        
        // Write data rows
        foreach (var item in data)
        {
            var values = createRowValues(item);
            writer.WriteLine(string.Join(",", values.Select(EscapeCsvField)));
        }
        
        writer.Flush();
        return Task.FromResult(memoryStream.ToArray());
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;
            
        // If the field contains a comma, quotes, or newline, wrap it in quotes and escape any quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
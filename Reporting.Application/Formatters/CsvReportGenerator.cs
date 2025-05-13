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
        logger.LogInformation("Generating CSV report for {Count} restaurants", statistics.Count());

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        
        // Write headers
        var headers = new[] {
            "Location", "Start Date", "End Date", "Waiter Name", "Waiter Email", 
            "Current Hours", "Previous Hours", "Delta Hours", 
            "Current Avg Service Feedback", "Previous Avg Service Feedback", 
            "Delta Avg Service Feedback", "Min Service Feedback"
        };
        
        writer.WriteLine(string.Join(",", headers.Select(EscapeCsvField)));
        
        // Write data rows
        foreach (var stat in statistics)
        {
            var deltaHours = ReportFormattingUtils.FormatPercentage(stat.DeltaHours);
            var deltaFeedback = ReportFormattingUtils.FormatPercentage(stat.DeltaAverageServiceFeedback);
            
            var fields = new[] {
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
            
            writer.WriteLine(string.Join(",", fields.Select(EscapeCsvField)));
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
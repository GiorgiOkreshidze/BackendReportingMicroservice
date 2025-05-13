using System.Globalization;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Logging;
using Reporting.Application.Formatters.Interfaces;
using Reporting.Application.Formatters.Utils;
using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters;

public class PdfReportGenerator(ILogger<ReportGenerator> logger) : IPdfReportGenerator
{
    public Task<byte[]> GenerateReportBytesAsync(IList<SummaryEntry> statistics)
    {
        logger.LogInformation("Generating PDF report for {Count} restaurants", statistics.Count());

        using var memoryStream = new MemoryStream();
        var writer = new PdfWriter(memoryStream);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf);

        // Add title
        document.Add(new Paragraph("Restaurant Report")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(14));

        // Create table with the appropriate number of columns
        var table = new Table(UnitValue.CreatePercentArray(12)).UseAllAvailableWidth();
        table.SetFontSize(8);
        
        // Add table headers
        string[] headers = {
            "Location", "Start Date", "End Date", "Waiter Name", "Waiter Email", 
            "Current Hours", "Previous Hours", "Delta Hours", 
            "Current Avg Service Feedback", "Previous Avg Service Feedback", 
            "Delta Avg Service Feedback", "Min Service Feedback"
        };

        foreach (var header in headers)
        {
            var paragraph = new Paragraph(header).AddStyle(new Style().SimulateBold());
            var headerCell = new Cell().Add(paragraph);
            table.AddHeaderCell(headerCell);
        }

        // Add data rows
        foreach (var stat in statistics)
        {
            // Format dates and numeric values
            var startDate = stat.StartDate;
            var endDate = stat.EndDate;
            var deltaHours = ReportFormattingUtils.FormatPercentage(stat.DeltaHours);
            var deltaFeedback = ReportFormattingUtils.FormatPercentage(stat.DeltaAverageServiceFeedback);

            table.AddCell(stat.Location);
            table.AddCell(startDate);
            table.AddCell(endDate);
            table.AddCell(stat.WaiterName);
            table.AddCell(stat.WaiterEmail);
            table.AddCell(stat.CurrentHours.ToString(CultureInfo.InvariantCulture));
            table.AddCell(stat.PreviousHours.ToString(CultureInfo.InvariantCulture));
            table.AddCell(deltaHours);
            table.AddCell(stat.CurrentAverageServiceFeedback.ToString(CultureInfo.InvariantCulture));
            table.AddCell(stat.PreviousAverageServiceFeedback.ToString(CultureInfo.InvariantCulture));
            table.AddCell(deltaFeedback);
            table.AddCell(stat.MinimumServiceFeedback.ToString(CultureInfo.InvariantCulture));
        }

        document.Add(table);
        document.Close();

        return Task.FromResult(memoryStream.ToArray());
    }
}
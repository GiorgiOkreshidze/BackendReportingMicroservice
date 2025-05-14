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
    string[] headers = {
        "Location", "Start Date", "End Date", "Waiter Name", "Waiter Email",
        "Current Hours", "Previous Hours", "Delta Hours",
        "Current Avg Service Feedback", "Previous Avg Service Feedback",
        "Delta Avg Service Feedback", "Min Service Feedback"
    };
    
    return GenerateReportAsync(statistics, "Restaurant Report", headers, stat => {
        var deltaHours = ReportFormattingUtils.FormatPercentageDouble(stat.DeltaHours);
        var deltaFeedback = ReportFormattingUtils.FormatPercentageDouble(stat.DeltaAverageServiceFeedback);
        
        return new Cell[] {
            new Cell().Add(new Paragraph(stat.Location)),
            new Cell().Add(new Paragraph(stat.StartDate)),
            new Cell().Add(new Paragraph(stat.EndDate)),
            new Cell().Add(new Paragraph(stat.WaiterName)),
            new Cell().Add(new Paragraph(stat.WaiterEmail)),
            new Cell().Add(new Paragraph(stat.CurrentHours.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(stat.PreviousHours.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(deltaHours)),
            new Cell().Add(new Paragraph(stat.CurrentAverageServiceFeedback.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(stat.PreviousAverageServiceFeedback.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(deltaFeedback)),
            new Cell().Add(new Paragraph(stat.MinimumServiceFeedback.ToString(CultureInfo.InvariantCulture)))
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
    
    return GenerateReportAsync(locationSummaries, "Location Sales Report", headers, stat => {
        var deltaOrders = ReportFormattingUtils.FormatPercentageDouble(stat.DeltaOrdersPercent);
        var deltaCuisineFeedback = ReportFormattingUtils.FormatPercentageDouble(stat.DeltaAvgCuisinePercent);
        var deltaRevenue = ReportFormattingUtils.FormatPercentageDecimal(stat.DeltaRevenuePercent);
        
        return new Cell[] {
            new Cell().Add(new Paragraph(stat.LocationName)),
            new Cell().Add(new Paragraph(stat.StartDate)),
            new Cell().Add(new Paragraph(stat.EndDate)),
            new Cell().Add(new Paragraph(stat.CurrentOrdersCount.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(stat.PreviousOrdersCount.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(deltaOrders)),
            new Cell().Add(new Paragraph(stat.CurrentAvgCuisineFeedback.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(stat.PreviousAvgCuisineFeedback.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(deltaCuisineFeedback)),
            new Cell().Add(new Paragraph(stat.CurrentMinCuisineFeedback.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(stat.CurrentRevenue.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(stat.PreviousRevenue.ToString(CultureInfo.InvariantCulture))),
            new Cell().Add(new Paragraph(deltaRevenue))
        };
    });
}
    
    private static Document CreatePdfDocument(MemoryStream memoryStream)
    {
        var writer = new PdfWriter(memoryStream);
        var pdf = new PdfDocument(writer);
        var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4.Rotate());
        document.SetMargins(20, 20, 20, 20);
        return document;
    }
    
    private static Table CreateHeaderTable(string[] headers, int columnCount)
    {
        var table = new Table(UnitValue.CreatePercentArray(columnCount)).UseAllAvailableWidth();
        table.SetFontSize(8);

        foreach (var header in headers)
        {
            var paragraph = new Paragraph(header).AddStyle(new Style().SimulateBold());
            var headerCell = new Cell().Add(paragraph);
            table.AddHeaderCell(headerCell);
        }
    
        return table;
    }
    
    private Task<byte[]> GenerateReportAsync<T>(
        IList<T> data, 
        string title,
        string[] headers, 
        Func<T, Cell[]> createRowCells)
    {
        logger.LogInformation("Generating PDF report for {Count} items", data.Count);

        using var memoryStream = new MemoryStream();
        var document = CreatePdfDocument(memoryStream);

        document.Add(new Paragraph(title)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(14));

        var table = CreateHeaderTable(headers, headers.Length);
    
        foreach (var item in data)
        {
            foreach (var cell in createRowCells(item))
            {
                table.AddCell(cell);
            }
        }

        document.Add(table);
        document.Close();

        return Task.FromResult(memoryStream.ToArray());
    }
}
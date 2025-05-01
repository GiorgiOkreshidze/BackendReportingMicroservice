using System.Drawing;
using System.Globalization;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Reporting.Application.Formatters.Interfaces;
using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters;

public class ReportGenerator(ILogger<ReportGenerator> logger) : IReportGenerator
{
    public Task<string> GenerateReportAsync(IList<SummaryEntry> statistics)
    {
        var bytes = GenerateExcelBytes(statistics);
        return Task.FromResult(Convert.ToBase64String(bytes));
    }
    
    
    public async Task<string> GenerateReportPDFAsync(IList<SummaryEntry> statistics)
    {
        var bytes = await GenerateReportBytesPdfAsync(statistics);
        return Convert.ToBase64String(bytes);
    }
    
    public Task<byte[]> GenerateReportBytesAsync(IList<SummaryEntry> statistics)
    {
        var bytes = GenerateExcelBytes(statistics);
        return Task.FromResult(bytes);
    }
    public Task<byte[]> GenerateReportBytesPdfAsync(IList<SummaryEntry> statistics)
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
            var deltaHours = FormatPercentage(stat.DeltaHours);
            var deltaFeedback = FormatPercentage(stat.DeltaAverageServiceFeedback);

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

    public Task<byte[]> GenerateReportBytesCsvAsync(IList<SummaryEntry> statistics)
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
        var deltaHours = FormatPercentage(stat.DeltaHours);
        var deltaFeedback = FormatPercentage(stat.DeltaAverageServiceFeedback);
        
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

// Helper method for properly escaping CSV fields
private string EscapeCsvField(string field)
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
    private static string FormatPercentage(double value)
    {
        return value >= 0 
            ? $"+{value:0.0%}" 
            : $"{value:0.0%}";
    }
    private byte[] GenerateExcelBytes(IList<SummaryEntry> statistics)
    {
        logger.LogInformation("Generating Excel report for {Count} restaurants", statistics.Count());

        // Set the EPPlus license context for non-commercial use
        ExcelPackage.License.SetNonCommercialPersonal("Report Generation");

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Restaurant Report");

        worksheet.Cells[1, 1].Value = "Location";
        worksheet.Cells[1, 2].Value = "Start Date";
        worksheet.Cells[1, 3].Value = "End Date";
        worksheet.Cells[1, 4].Value = "Waiter Name";
        worksheet.Cells[1, 5].Value = "Waiter Email";
        worksheet.Cells[1, 6].Value = "Current Hours";
        worksheet.Cells[1, 7].Value = "Previous Hours";
        worksheet.Cells[1, 8].Value = "Delta Hours";
        worksheet.Cells[1, 9].Value = "Current Average Service Feedback Waiter";
        worksheet.Cells[1, 10].Value = "Previous Average Service Feedback Waiter";
        worksheet.Cells[1, 11].Value = "Delta Average Service Feedback Waiter";
        worksheet.Cells[1, 12].Value = "Minimum Service Feedback Location";

        using (var range = worksheet.Cells[1, 1, 1, 12]) 
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        }

        int row = 2;
        foreach (var stat in statistics)
        {
            worksheet.Cells[row, 1].Value = stat.Location;
            worksheet.Cells[row, 2].Value = stat.StartDate;
            worksheet.Cells[row, 3].Value = stat.EndDate;
            worksheet.Cells[row, 4].Value = stat.WaiterName;
            worksheet.Cells[row, 5].Value = stat.WaiterEmail;
            worksheet.Cells[row, 6].Value = stat.CurrentHours;
            worksheet.Cells[row, 7].Value = stat.PreviousHours;
            worksheet.Cells[row, 8].Value = stat.DeltaHours;
            ApplyPercentageFormat(worksheet.Cells[row, 8]);
            worksheet.Cells[row, 9].Value = stat.CurrentAverageServiceFeedback;
            worksheet.Cells[row, 10].Value = stat.PreviousAverageServiceFeedback;
            worksheet.Cells[row, 11].Value = stat.DeltaAverageServiceFeedback;
            ApplyPercentageFormat(worksheet.Cells[row, 11]);
            worksheet.Cells[row, 12].Value = stat.MinimumServiceFeedback;
            row++;
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    private static void ApplyPercentageFormat(ExcelRange cell)
    {
        cell.Style.Numberformat.Format = "+0.0%;-0.0%";
    }
}

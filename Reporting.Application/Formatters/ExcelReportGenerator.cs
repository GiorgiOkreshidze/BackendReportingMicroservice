using System.Drawing;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Reporting.Application.Formatters.Interfaces;
using Reporting.Application.Formatters.Utils;
using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters;

public class ExcelReportGenerator(ILogger<ExcelReportGenerator> logger) : IExcelReportGenerator
{
    private const string LicenseContext = "Report Generation";
    private const string RestaurantReportSheetName = "Restaurant Report";
    private const string LocationReportSheetName = "Location Report";

    private sealed record ReportColumnConfig(
        string Header,
        Action<ExcelRange, object> SetValue,
        Action<ExcelRange>? ApplyFormat = null);

    public byte[] GenerateReportBytes(IList<SummaryEntry> statistics)
    {
        if (statistics == null || statistics.Count == 0)
        {
            logger.LogWarning("No statistics provided for restaurant report generation.");
            return Array.Empty<byte>();
        }

        logger.LogInformation("Generating Excel report for {Count} restaurants", statistics.Count);

        var columns = new List<ReportColumnConfig>
        {
            new("Location", (cell, data) => cell.Value = ((SummaryEntry)data).Location),
            new("Start Date", (cell, data) => cell.Value = ((SummaryEntry)data).StartDate),
            new("End Date", (cell, data) => cell.Value = ((SummaryEntry)data).EndDate),
            new("Waiter Name", (cell, data) => cell.Value = ((SummaryEntry)data).WaiterName),
            new("Waiter Email", (cell, data) => cell.Value = ((SummaryEntry)data).WaiterEmail),
            new("Current Hours", (cell, data) => cell.Value = ((SummaryEntry)data).CurrentHours),
            new("Previous Hours", (cell, data) => cell.Value = ((SummaryEntry)data).PreviousHours),
            new("Delta Hours", (cell, data) => cell.Value = ((SummaryEntry)data).DeltaHours,
                ReportFormattingUtils.ApplyPercentageFormat),
            new("Current Avg Service Feedback Waiter",
                (cell, data) => cell.Value = ((SummaryEntry)data).CurrentAverageServiceFeedback),
            new("Previous Avg Service Feedback Waiter",
                (cell, data) => cell.Value = ((SummaryEntry)data).PreviousAverageServiceFeedback),
            new("Delta Avg Service Feedback Waiter",
                (cell, data) => cell.Value = ((SummaryEntry)data).DeltaAverageServiceFeedback,
                ReportFormattingUtils.ApplyPercentageFormat),
            new("Minimum Service Feedback Location",
                (cell, data) => cell.Value = ((SummaryEntry)data).MinimumServiceFeedback)
        };

        return GenerateExcelReport(statistics, RestaurantReportSheetName, columns);
    }

    public byte[] GenerateReportBytesOfLocationSummaries(IList<LocationSummary> locationSummaries)
    {
        if (locationSummaries == null || locationSummaries.Count == 0)
        {
            logger.LogWarning("No location summaries provided for location report generation.");
            return Array.Empty<byte>();
        }

        logger.LogInformation("Generating Excel report for {Count} locations", locationSummaries.Count);

        var columns = new List<ReportColumnConfig>
        {
            new("Location", (cell, data) => cell.Value = ((LocationSummary)data).LocationName),
            new("Start Date", (cell, data) => cell.Value = ((LocationSummary)data).StartDate),
            new("End Date", (cell, data) => cell.Value = ((LocationSummary)data).EndDate),
            new("Current Orders Count", (cell, data) => cell.Value = ((LocationSummary)data).CurrentOrdersCount),
            new("Previous Orders Count", (cell, data) => cell.Value = ((LocationSummary)data).PreviousOrdersCount),
            new("Delta Orders %", (cell, data) => cell.Value = ((LocationSummary)data).DeltaOrdersPercent,
                ReportFormattingUtils.ApplyPercentageFormat),
            new("Current Avg Cuisine Feedback",
                (cell, data) => cell.Value = ((LocationSummary)data).CurrentAvgCuisineFeedback),
            new("Previous Avg Cuisine Feedback",
                (cell, data) => cell.Value = ((LocationSummary)data).PreviousAvgCuisineFeedback),
            new("Delta Avg Cuisine %", (cell, data) => cell.Value = ((LocationSummary)data).DeltaAvgCuisinePercent,
                ReportFormattingUtils.ApplyPercentageFormat),
            new("Current Min Cuisine Feedback",
                (cell, data) => cell.Value = ((LocationSummary)data).CurrentMinCuisineFeedback),
            new("Current Revenue", (cell, data) => cell.Value = ((LocationSummary)data).CurrentRevenue),
            new("Previous Revenue", (cell, data) => cell.Value = ((LocationSummary)data).PreviousRevenue),
            new("Delta Revenue %", (cell, data) => cell.Value = ((LocationSummary)data).DeltaRevenuePercent,
                ReportFormattingUtils.ApplyPercentageFormat)
        };

        return GenerateExcelReport(locationSummaries, LocationReportSheetName, columns);
    }

    private static byte[] GenerateExcelReport<T>(IList<T> data, string sheetName, List<ReportColumnConfig> columns)
    {
        ExcelPackage.License.SetNonCommercialPersonal(LicenseContext);

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        // Set headers
        for (int col = 1; col <= columns.Count; col++)
        {
            worksheet.Cells[1, col].Value = columns[col - 1].Header;
        }

        // Format header row
        FormatHeaderRow(worksheet, 1, columns.Count);

        // Populate data rows
        int row = 2;
        foreach (var item in data)
        {
            for (int col = 1; col <= columns.Count; col++)
            {
                var config = columns[col - 1];
                var cell = worksheet.Cells[row, col];
                config.SetValue(cell, item);
                config.ApplyFormat?.Invoke(cell);
            }

            row++;
        }

        worksheet.Cells.AutoFitColumns();

        return package.GetAsByteArray();
    }

    private static void FormatHeaderRow(ExcelWorksheet worksheet, int row, int columnCount)
    {
        using var range = worksheet.Cells[row, 1, row, columnCount];
        range.Style.Font.Bold = true;
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
    }
}
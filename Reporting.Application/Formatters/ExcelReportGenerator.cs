using System.Drawing;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Reporting.Application.Formatters.Interfaces;
using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters;

public class ExcelReportGenerator(ILogger<ExcelReportGenerator> logger) : IReportGenerator
{
    public Task<string> GenerateReportAsync(IList<SummaryEntry> statistics)
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

        var excelBytes = package.GetAsByteArray();
        return Task.FromResult(Convert.ToBase64String(excelBytes));
    }

    private static void ApplyPercentageFormat(ExcelRange cell)
    {
        cell.Style.Numberformat.Format = "+0.0%;-0.0%";
    }
}
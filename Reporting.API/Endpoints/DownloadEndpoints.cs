using Reporting.Application.DTOs;
using Reporting.Application.Formatters.Interfaces;
using Reporting.Application.Interfaces;
using Reporting.Domain.Entities;

namespace Reporting.API.Endpoints;

public class DownloadEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        app.MapGet("/reports/download", DownloadReport)
            .WithTags("Reports")
            .WithDescription("Download a report in the specified format");
    }
    
    private static async Task<IResult> DownloadReport(
        IReportServiceSender reportSenderService,
        IReportGenerator reportGenerator,
        ILogger<DownloadEndpoints> logger,
        [AsParameters] ReportDownloadRequest request)
    {
      
        try
        {
            // Validate required parameters
            if (request.StartDate == null || request.EndDate == null)
            {
                return Results.BadRequest( new { Message = "Both startDate and endDate parameters are required"});
            }
            
            if (!TryParseIsoDate(request.StartDate, out DateTime startDate))
            {
                return Results.BadRequest(new { Message ="startDate must be in ISO format (YYYY-MM-DD)"});
            }
        
            if (!TryParseIsoDate(request.EndDate, out DateTime endDate))
            {
                return Results.BadRequest(new { Message ="endDate must be in ISO format (YYYY-MM-DD)"});
            }

            if (string.IsNullOrEmpty(request.Format) || 
                !new[] { "excel", "pdf", "csv" }.Contains(request.Format.ToLower()))
            {
                return Results.BadRequest(new { Message ="Format must be one of: excel, pdf, csv"});
            }

            logger.LogInformation("Generating {Format} report from {StartDate} to {EndDate}{LocationFilter}",
                request.Format,
                request.StartDate, 
                request.EndDate,
                !string.IsNullOrEmpty(request.LocationId) ? $" for location '{request.LocationId}'" : "");

            // Get the report data
            var reportData = await reportSenderService.SendReportToAdminAsync(
                startDate,
                endDate,
                request.LocationId);

            return await GenerateAndReturnFile(reportData, request, reportGenerator);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request parameters for report download");
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate downloadable report");
            return Results.Problem("Failed to generate report.", statusCode: 500);
        }
    }
    private static bool TryParseIsoDate(string dateString, out DateTime date)
    {
        // ISO format YYYY-MM-DD
        return DateTime.TryParseExact(
            dateString,
            "yyyy-MM-dd", 
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, 
            out date);
    }
    private static async Task<IResult> GenerateAndReturnFile(
        (List<SummaryEntry> WaiterSummaries, List<LocationSummary> LocationSummaries) reportData,
        ReportDownloadRequest request, 
        IReportGenerator reportGenerator)
    {
        if (string.IsNullOrEmpty(request.ReportType) || 
            !new[] { "sales", "performance" }.Contains(request.ReportType.ToLower()))
        {
            return Results.BadRequest(new { Message = "ReportType must be either 'sales' or 'performance'" });
        }

        bool isSalesReport = request.ReportType.ToLower() == "sales";
        string dateRange = $"{request.StartDate}_to_{request.EndDate}";
        
        byte[] fileBytes;
        string fileName;
        string contentType;
        
        switch (request.Format!.ToLower())
        {
            case "excel":
                fileBytes = isSalesReport
                    ? await reportGenerator.GenerateReportBytesOfLocationSummariesAsync(reportData.LocationSummaries)
                    : await reportGenerator.GenerateReportBytesAsync(reportData.WaiterSummaries);
                
                fileName = isSalesReport
                    ? $"location_report_{dateRange}.xlsx"
                    : $"waiter_report_{dateRange}.xlsx";
                
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                break;
                
            case "pdf":
                fileBytes = isSalesReport
                    ? await reportGenerator.GenerateReportBytesOfLocationSummariesPdfAsync(reportData.LocationSummaries)
                    : await reportGenerator.GenerateReportBytesPdfAsync(reportData.WaiterSummaries);
                
                fileName = isSalesReport
                    ? $"location_report_{dateRange}.pdf"
                    : $"waiter_report_{dateRange}.pdf";
                
                contentType = "application/pdf";
                break;

            case "csv":
                fileBytes = isSalesReport
                    ? await reportGenerator.GenerateReportBytesOfLocationSummariesCsvAsync(reportData.LocationSummaries)
                    : await reportGenerator.GenerateReportBytesCsvAsync(reportData.WaiterSummaries);
                
                fileName = isSalesReport
                    ? $"location_report_{dateRange}.csv" // Fixed extension (was pdf)
                    : $"waiter_report_{dateRange}.csv";
                contentType = "text/csv";
                break;
                
            default:
                return Results.BadRequest(new { Message = "Unsupported Format" });
        }

        return Results.File(fileBytes, contentType, fileName);
    }
}
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
        List<SummaryEntry> reportData, 
        ReportDownloadRequest request, 
        IReportGenerator reportGenerator)
    {
        byte[] fileBytes;
        string fileName;
        string contentType;
        
        switch (request.Format!.ToLower())
        {
            case "excel":
                fileBytes = await reportGenerator.GenerateReportBytesAsync(reportData);
                fileName = $"report_{request.StartDate:yyyyMMdd}_to_{request.EndDate:yyyyMMdd}.xlsx";
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                break;
                
            case "pdf":
                fileBytes = await reportGenerator.GenerateReportBytesPdfAsync(reportData);
                fileName = $"report_{request.StartDate:yyyyMMdd}_to_{request.EndDate:yyyyMMdd}.pdf";
                contentType = "application/pdf";
                break;
                
            case "csv":
                fileBytes = await reportGenerator.GenerateReportBytesCsvAsync(reportData);
                fileName = $"report_{request.StartDate:yyyyMMdd}_to_{request.EndDate:yyyyMMdd}.csv";
                contentType = "text/csv";
                break;
                
            default:
                return Results.BadRequest("Unsupported format");
        }

        return Results.File(fileBytes, contentType, fileName);
    }
}
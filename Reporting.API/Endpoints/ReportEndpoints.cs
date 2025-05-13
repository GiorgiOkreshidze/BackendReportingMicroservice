using Reporting.Application.DTOs;
using Reporting.Application.Interfaces;

namespace Reporting.API.Endpoints;

public class ReportEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/reports").WithTags("Reports");
        
        group.MapGet("/", GetReports)
            .WithDescription("Get reports by date range and optional location and report type");
             
        group.MapPost("/send", SendReport)
            .WithDescription("Manually trigger report sending");
    }
    
    private static async Task<IResult> GetReports(
        IReportServiceSender reportSenderService, 
        ILogger<ReportEndpoints> logger,
        [AsParameters] ReportRequest request)
    {
        try
        {
            // Validate required parameters
            if (request.StartDate == null || request.EndDate == null)
            {
                return Results.BadRequest(new { Message = "Both startDate and endDate parameters are required"});
            }

            if (!TryParseIsoDate(request.StartDate, out DateTime startDate))
            {
                return Results.BadRequest(new { Message ="startDate must be in ISO format (YYYY-MM-DD)"});
            }
        
            if (!TryParseIsoDate(request.EndDate, out DateTime endDate))
            {
                return Results.BadRequest(new { Message ="endDate must be in ISO format (YYYY-MM-DD)"});
            }
            
            // Validate ReportType
            if (!string.IsNullOrEmpty(request.ReportType) && 
                request.ReportType != "Sales" && 
                request.ReportType != "Performance")
            {
                return Results.BadRequest(new { Message = "ReportType must be either 'Sales' or 'Performance'" });
            }
            
            logger.LogInformation("Retrieving reports from {StartDate} to {EndDate}{LocationFilter}",
                request.StartDate, request.EndDate, 
                !string.IsNullOrEmpty(request.LocationId) ? $" for location '{request.LocationId}'" : "");

            var result = await reportSenderService.SendReportToAdminAsync(
                startDate, 
                endDate, 
                request.LocationId);
            
            if (string.IsNullOrEmpty(request.ReportType))
            {
                return Results.Ok(new
                {
                    Sales = result.LocationSummaries,
                    Performance = result.WaiterSummaries
                });
            }
            
            return request.ReportType == "Sales" 
                ? Results.Ok(result.LocationSummaries) 
                : Results.Ok(result.WaiterSummaries);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid request parameters for /reports endpoint");
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve reports via /reports endpoint");
            return Results.Problem("Failed to retrieve reports.", statusCode: 500);
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
    private static async Task<IResult> SendReport(
        IReportServiceSender reportSenderService, 
        ILogger<ReportEndpoints> logger)
    {
        try
        {
            logger.LogInformation("Received request to send report");
            await reportSenderService.SendReportEmailAsync();
            return Results.Ok(new {Message = "Report sent successfully."});
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send report");
            return Results.Problem("Failed to send report.", statusCode: 500);
        }
    }
}
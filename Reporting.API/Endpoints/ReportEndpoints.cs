using Reporting.Application.DTOs;
using Reporting.Application.Interfaces;

namespace Reporting.API.Endpoints;

public class ReportEndpoints
{
    public static void MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/reports").WithTags("Reports");
        
        group.MapGet("/", GetReports)
            .WithDescription("Get reports by date range and optional location");
             
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
                return Results.BadRequest("Both startDate and endDate parameters are required");
            }

            logger.LogInformation("Retrieving reports from {StartDate} to {EndDate}{LocationFilter}",
                request.StartDate, request.EndDate, 
                !string.IsNullOrEmpty(request.LocationId) ? $" for location '{request.LocationId}'" : "");

            var result = await reportSenderService.SendReportToAdminAsync(
                request.StartDate.Value, 
                request.EndDate.Value, 
                request.LocationId);
                
            return Results.Ok(result);
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
    
    private static async Task<IResult> SendReport(
        IReportServiceSender reportSenderService, 
        ILogger<ReportEndpoints> logger)
    {
        try
        {
            logger.LogInformation("Received request to send report");
            await reportSenderService.SendReportEmailAsync();
            return Results.Ok("Report sent successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send report");
            return Results.Problem("Failed to send report.", statusCode: 500);
        }
    }
}
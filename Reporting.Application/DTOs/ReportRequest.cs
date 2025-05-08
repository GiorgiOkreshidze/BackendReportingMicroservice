namespace Reporting.Application.DTOs;

public class ReportRequest
{
    public string? StartDate { get; set; }
    
    public string? EndDate { get; set; }
    
    public string? LocationId { get; set; }
}
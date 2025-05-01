namespace Reporting.Application.DTOs;

public class ReportRequest
{
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public string? LocationId { get; set; }
}
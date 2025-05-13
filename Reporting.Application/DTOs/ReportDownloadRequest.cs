namespace Reporting.Application.DTOs;

public class ReportDownloadRequest
{
    public string? StartDate { get; set; }
    
    public string? EndDate { get; set; }
    
    public string? LocationId { get; set; }
    
    public string? ReportType { get; set; } // "waiter" or "location"
    
    public string? Format { get; set; } // "excel", "pdf", or "csv"
}
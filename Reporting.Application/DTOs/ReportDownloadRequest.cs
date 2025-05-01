namespace Reporting.Application.DTOs;

public class ReportDownloadRequest
{
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public string? LocationId { get; set; }
    
    public string? Format { get; set; } // "excel", "pdf", or "csv"
}
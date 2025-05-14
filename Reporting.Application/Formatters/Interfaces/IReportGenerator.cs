using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters.Interfaces;

public interface IReportGenerator
{
    Task<string> GenerateWaiterReportAsync(IList<SummaryEntry> statistics);

    Task<string> GenerateLocationReportAsync(List<LocationSummary> locationSummaries);
    
    Task<byte[]> GenerateReportBytesAsync(IList<SummaryEntry> statistics);
    
    Task<byte[]> GenerateReportBytesOfLocationSummariesAsync(IList<LocationSummary> locationSummaries);
    
    Task<byte[]> GenerateReportBytesPdfAsync(IList<SummaryEntry> statistics);

    Task<byte[]> GenerateReportBytesOfLocationSummariesPdfAsync(IList<LocationSummary> statistics);
    
    Task<byte[]> GenerateReportBytesCsvAsync(IList<SummaryEntry> statistics);
    
    Task<byte[]> GenerateReportBytesOfLocationSummariesCsvAsync(IList<LocationSummary> statistics);
}
using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters.Interfaces;

public interface IReportGenerator
{
    Task<string> GenerateReportAsync(IList<SummaryEntry> statistics);

    Task<byte[]> GenerateReportBytesAsync(IList<SummaryEntry> statistics);

    Task<string> GenerateReportPDFAsync(IList<SummaryEntry> statistics);
    
    Task<byte[]> GenerateReportBytesPdfAsync(IList<SummaryEntry> statistics);
    
    Task<byte[]> GenerateReportBytesCsvAsync(IList<SummaryEntry> statistics);
}
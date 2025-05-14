using Reporting.Application.Formatters.Interfaces;
using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters;

public class ReportGenerator(
    IPdfReportGenerator pdfReportGenerator, 
    IExcelReportGenerator excelReportGenerator, 
    ICsvReportGenerator csvReportGenerator
    ) : IReportGenerator
{
    public Task<string> GenerateWaiterReportAsync(IList<SummaryEntry> statistics)
    {
        var bytes = excelReportGenerator.GenerateReportBytes(statistics);
        return Task.FromResult(Convert.ToBase64String(bytes));
    }

    public Task<string> GenerateLocationReportAsync(List<LocationSummary> locationSummaries)
    {
        var bytes = excelReportGenerator.GenerateReportBytesOfLocationSummaries(locationSummaries);
        return Task.FromResult(Convert.ToBase64String(bytes));
    }
    
    public Task<byte[]> GenerateReportBytesAsync(IList<SummaryEntry> statistics)
    {
        var bytes = excelReportGenerator.GenerateReportBytes(statistics);
        return Task.FromResult(bytes);
    }
    
    public Task<byte[]> GenerateReportBytesOfLocationSummariesAsync(IList<LocationSummary> locationSummaries)
    {
        var bytes = excelReportGenerator.GenerateReportBytesOfLocationSummaries(locationSummaries);
        return Task.FromResult(bytes);
    }

    public Task<byte[]> GenerateReportBytesPdfAsync(IList<SummaryEntry> statistics)
    {
        return pdfReportGenerator.GenerateReportBytesAsync(statistics);
    }
    
    public Task<byte[]> GenerateReportBytesOfLocationSummariesPdfAsync(IList<LocationSummary> statistics)
    {
        return pdfReportGenerator.GenerateReportBytesLocationSummariesAsync(statistics);
    }

    public Task<byte[]> GenerateReportBytesCsvAsync(IList<SummaryEntry> statistics)
    {
        return csvReportGenerator.GenerateReportBytesAsync(statistics);
    }
    
    public Task<byte[]> GenerateReportBytesOfLocationSummariesCsvAsync(IList<LocationSummary> statistics)
    {
        return csvReportGenerator.GenerateReportBytesLocationSummariesAsync(statistics);
    }
}

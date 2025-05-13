using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters.Interfaces;

public interface IExcelReportGenerator
{
    byte[] GenerateReportBytes(IList<SummaryEntry> statistics);
    
    byte[] GenerateReportBytesOfLocationSummaries(IList<LocationSummary> locationSummaries);
}
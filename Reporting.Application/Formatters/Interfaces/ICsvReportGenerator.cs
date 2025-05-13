using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters.Interfaces;

public interface ICsvReportGenerator
{
    Task<byte[]> GenerateReportBytesAsync(IList<SummaryEntry> statistics);
}
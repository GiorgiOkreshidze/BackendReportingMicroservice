using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters.Interfaces;

public interface IPdfReportGenerator
{
    Task<byte[]> GenerateReportBytesAsync(IList<SummaryEntry> statistics);
}
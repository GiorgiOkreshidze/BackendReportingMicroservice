using Reporting.Domain.Entities;

namespace Reporting.Application.Formatters.Interfaces;

public interface IReportGenerator
{
    Task<string> GenerateReportAsync(IList<SummaryEntry> statistics);
}
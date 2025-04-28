using Reporting.Domain.Entities;

namespace Reporting.Infrastructure.Repositories.Interfaces;

public interface IReportRepository
{
    Task<List<Report>> RetrieveReports(DateTime startDate, DateTime endDate);

    Task SaveReportAsync(Report report, CancellationToken cancellationToken = default);

}
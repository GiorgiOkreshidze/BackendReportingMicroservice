using Reporting.Domain.Entities;

namespace Reporting.Infrastructure.Repositories.Interfaces;

public interface IReportRepository
{
    Task<List<Report>> RetrieveReports(DateTime startDate, DateTime endDate);

    Task SaveReportAsync(Report report, CancellationToken cancellationToken = default);

    Task<IEnumerable<Report>> RetrieveReportsForAdmin(DateTime startDate, DateTime endDate, string? locationId);

}
using Reporting.Domain.Entities;

namespace Reporting.Application.Interfaces;

public interface IReportProcessService
{
    Task<(List<SummaryEntry> WaiterSummaries, List<LocationSummary> LocationSummaries)> ProcessReports(List<Report> currentWeek, List<Report> previousWeek, DateTime startDate,
        DateTime endDate);
}
using Reporting.Domain.Entities;

namespace Reporting.Application.Interfaces;

public interface IReportProcessService
{
    List<SummaryEntry> ProcessReports(List<Report> currentWeek, List<Report> previousWeek, DateTime startDate,
        DateTime endDate);
}
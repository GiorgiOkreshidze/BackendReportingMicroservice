using Reporting.Domain.Entities;

namespace Reporting.Application.Interfaces;

public interface IReportServiceSender
{
   Task SendReportEmailAsync();

    Task<List<SummaryEntry>> SendReportToAdminAsync(DateTime startDate, DateTime endDate, string? locationId);
}
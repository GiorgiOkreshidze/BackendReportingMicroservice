using Reporting.Domain.Entities;

namespace Reporting.Application.Interfaces;

public interface IReportServiceSender
{
   Task SendReportEmailAsync();

   Task<(List<SummaryEntry> WaiterSummaries, List<LocationSummary> LocationSummaries)> SendReportToAdminAsync(DateTime startDate, DateTime endDate, string? locationId);
}
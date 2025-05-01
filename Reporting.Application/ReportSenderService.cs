using Reporting.Application.Formatters.Interfaces;
using Reporting.Application.Interfaces;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Repositories.Interfaces;

namespace Reporting.Application;

public class ReportSenderService(
    IReportRepository reportRepository,
    IReportGenerator reportGenerator,
    IReportProcessService reportProcessService,
    IEmailSenderService emailSenderService) : IReportServiceSender
{
    public async Task SendReportEmailAsync()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var currentWeekStart = yesterday.AddDays(-6);
        var currentWeekEnd = yesterday;
        var previousWeekStart = currentWeekStart.AddDays(-7);
        var previousWeekEnd = currentWeekEnd.AddDays(-7);
Console.WriteLine($"week start email to {currentWeekStart}");
        var currentWeekItems = await reportRepository.RetrieveReports(currentWeekStart, currentWeekEnd);
        var previousWeekItems = await reportRepository.RetrieveReports(previousWeekStart, previousWeekEnd);

        var summary = reportProcessService.ProcessReports(currentWeekItems, previousWeekItems, currentWeekStart, currentWeekEnd);

        var reportContent = await reportGenerator.GenerateReportAsync(summary);

        const string fileName = "report.xlsx";
        const string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        await emailSenderService.SendEmailAsync(reportContent, fileName, mimeType);
    }

    public async Task<List<SummaryEntry>> SendReportToAdminAsync(DateTime startDate, DateTime endDate, string? locationId)
    {
        // Validate the date range
        if (startDate >= endDate)
        {
            throw new ArgumentException("Start date must be before end date");
        }

        if (endDate > DateTime.UtcNow.Date)
        {
            throw new ArgumentException("End date cannot be in the future");
        }

        if (startDate.Date == endDate.Date)
        {
            throw new ArgumentException("Start date and end date cannot be the same day");
        }

        // Calculate the date range length
        int rangeDays = (endDate.Date - startDate.Date).Days;
        
        // Calculate previous period with exactly the same length
        var previousPeriodEnd = startDate.Date.AddDays(-1);
        var previousPeriodStart = previousPeriodEnd.AddDays(-rangeDays);

        // Retrieve reports for both current and previous periods
        var currentPeriodReports = await reportRepository.RetrieveReportsForAdmin(startDate, endDate, locationId);
        var previousPeriodReports = await reportRepository.RetrieveReportsForAdmin(previousPeriodStart, previousPeriodEnd, locationId);

        // Process the reports to create a summary
        var summary = reportProcessService.ProcessReports(
            currentPeriodReports.ToList(), 
            previousPeriodReports.ToList(), 
            startDate, 
            endDate);

        // Generate the report content
        return summary;
    }
}

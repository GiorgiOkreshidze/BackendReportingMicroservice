using Reporting.Application.Formatters.Interfaces;
using Reporting.Application.Interfaces;
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
        var currentWeekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
        var currentWeekEnd = currentWeekStart.AddDays(6);
        var previousWeekStart = currentWeekStart.AddDays(-7);
        var previousWeekEnd = currentWeekEnd.AddDays(-7);

        var currentWeekItems = await reportRepository.RetrieveReports(currentWeekStart, currentWeekEnd);
        var previousWeekItems = await reportRepository.RetrieveReports(previousWeekStart, previousWeekEnd);

        var summary = reportProcessService.ProcessReports(currentWeekItems, previousWeekItems, currentWeekStart, currentWeekEnd);

        var reportContent = await reportGenerator.GenerateReportAsync(summary);

        const string fileName = "report.xlsx";
        const string mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        await emailSenderService.SendEmailAsync(reportContent, fileName, mimeType);
    }
}
namespace Reporting.Application.Interfaces;

public interface IReportServiceSender
{
    public Task SendReportEmailAsync();
}
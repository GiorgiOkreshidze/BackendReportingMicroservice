namespace Reporting.Application.Interfaces;

public interface IEmailSenderService
{
    Task SendEmailAsync(string reportContent, string fileName, string mimeType);
}
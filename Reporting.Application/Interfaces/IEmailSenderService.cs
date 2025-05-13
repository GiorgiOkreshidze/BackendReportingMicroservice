namespace Reporting.Application.Interfaces;

public interface IEmailSenderService
{
    Task SendEmailAsync(string reportContent, string fileName, string mimeType);

    Task SendEmailWithMultipleAttachmentsAsync(List<(string Content, string FileName, string MimeType)> attachments);
}
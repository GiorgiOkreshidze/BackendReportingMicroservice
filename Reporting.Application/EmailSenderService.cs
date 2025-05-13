using System.Text;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reporting.Application.DTOs.Emails;
using Reporting.Application.Interfaces;

namespace Reporting.Application;

public class EmailSenderService(IAmazonSimpleEmailService sesClient, ILogger<ReportSenderService> logger, IOptions<EmailSettings> emailSettings) : IEmailSenderService
{
    public async Task SendEmailAsync(string reportContent, string fileName, string mimeType)
    {
        try
        {
            MemoryStream attachmentStream = mimeType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                ? new MemoryStream(Convert.FromBase64String(reportContent))
                : new MemoryStream(Encoding.UTF8.GetBytes(reportContent));

            using (attachmentStream)
            {
                var request = new SendRawEmailRequest
                {
                    RawMessage = new RawMessage
                    {
                        Data = CreateEmailMessage(attachmentStream, fileName, mimeType)
                    }
                };
                await sesClient.SendRawEmailAsync(request);
                logger.LogInformation("Email sent successfully with attachment {FileName}", fileName);
            }
        }
        catch (Exception)
        {
            logger.LogError("Failed to send email with attachment {FileName}", fileName);
            throw; // Re-throw to allow caller to handle or retry
        }
    }

    private MemoryStream CreateEmailMessage(MemoryStream attachment, string fileName, string mimeType)
    {
        var boundary = "----=_Part_" + Guid.NewGuid().ToString();
        var emailBuilder = new StringBuilder();
        
        AppendHeaders(emailBuilder, boundary, "Weekly Report Summary");
        AppendTextBody(emailBuilder, boundary, "Please find the attached weekly report.");
        AppendAttachmentHeader(emailBuilder, boundary, mimeType, fileName);

        emailBuilder.AppendLine(Convert.ToBase64String(attachment.ToArray(), Base64FormattingOptions.InsertLineBreaks));
        emailBuilder.AppendLine();
        emailBuilder.AppendLine($"--{boundary}--");

        return new MemoryStream(Encoding.UTF8.GetBytes(emailBuilder.ToString()));
    }

    public async Task SendEmailWithMultipleAttachmentsAsync(List<(string Content, string FileName, string MimeType)> attachments)
    {
        try
        {
            var request = new SendRawEmailRequest
            {
                RawMessage = new RawMessage
                {
                    Data = CreateEmailMessageWithMultipleAttachments(attachments)
                }
            };
            
            await sesClient.SendRawEmailAsync(request);
            logger.LogInformation("Email sent successfully with {Count} attachments", attachments.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email with multiple attachments");
            throw; // Re-throw to allow caller to handle or retry
        }
    }

    private MemoryStream CreateEmailMessageWithMultipleAttachments(List<(string Content, string FileName, string MimeType)> attachments)
    {
        var boundary = "----=_Part_" + Guid.NewGuid().ToString();
        var emailBuilder = new StringBuilder();
        
        AppendHeaders(emailBuilder, boundary, "Weekly Restaurant Reports");
        AppendTextBody(emailBuilder, boundary, "Please find the attached weekly restaurant report.");
        
        // Add each attachment
        foreach (var (content, fileName, mimeType) in attachments)
        {
            AppendAttachmentHeader(emailBuilder, boundary, mimeType, fileName);

            // Convert content to base64
            byte[] fileBytes = mimeType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                ? Convert.FromBase64String(content)
                : Encoding.UTF8.GetBytes(content);
                
            emailBuilder.AppendLine(Convert.ToBase64String(fileBytes, Base64FormattingOptions.InsertLineBreaks));
            emailBuilder.AppendLine();
        }
        
        // Close the boundary
        emailBuilder.AppendLine($"--{boundary}--");

        return new MemoryStream(Encoding.UTF8.GetBytes(emailBuilder.ToString()));
    }

    private static void AppendAttachmentHeader(StringBuilder emailBuilder, string boundary, string mimeType,
        string fileName)
    {
        emailBuilder.AppendLine($"--{boundary}");
        emailBuilder.AppendLine($"Content-Type: {mimeType}; name=\"{fileName}\"");
        emailBuilder.AppendLine("Content-Disposition: attachment; filename=\"" + fileName + "\"");
        emailBuilder.AppendLine("Content-Transfer-Encoding: base64");
        emailBuilder.AppendLine();
    }

    private void AppendHeaders(StringBuilder emailBuilder, string boundary, string subject)
    {
        emailBuilder.AppendLine($"From: {emailSettings.Value.FromEmail}");
        emailBuilder.AppendLine($"To: {emailSettings.Value.ToEmail}");
        emailBuilder.AppendLine($"Subject: {subject}");
        emailBuilder.AppendLine($"MIME-Version: 1.0");
        emailBuilder.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        emailBuilder.AppendLine();
    }
    
    private static void AppendTextBody(StringBuilder builder, string boundary, string bodyText)
    {
        builder.AppendLine($"--{boundary}");
        builder.AppendLine("Content-Type: text/plain; charset=UTF-8");
        builder.AppendLine();
        builder.AppendLine(bodyText);
        builder.AppendLine();
    }
}
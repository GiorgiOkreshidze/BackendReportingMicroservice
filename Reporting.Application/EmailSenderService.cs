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
        emailBuilder.AppendLine($"From: {emailSettings.Value.FromEmail}");
        emailBuilder.AppendLine($"To: {emailSettings.Value.ToEmail}");
        emailBuilder.AppendLine("Subject: Weekly Report Summary");
        emailBuilder.AppendLine($"MIME-Version: 1.0");
        emailBuilder.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        emailBuilder.AppendLine();
        emailBuilder.AppendLine($"--{boundary}");
        emailBuilder.AppendLine("Content-Type: text/plain; charset=UTF-8");
        emailBuilder.AppendLine();
        emailBuilder.AppendLine("Please find the attached weekly report.");
        emailBuilder.AppendLine();
        emailBuilder.AppendLine($"--{boundary}");
        emailBuilder.AppendLine($"Content-Type: {mimeType}; name=\"{fileName}\"");
        emailBuilder.AppendLine("Content-Disposition: attachment; filename=\"" + fileName + "\"");
        emailBuilder.AppendLine("Content-Transfer-Encoding: base64");
        emailBuilder.AppendLine();
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
        
        // Email headers
        emailBuilder.AppendLine($"From: {emailSettings.Value.FromEmail}");
        emailBuilder.AppendLine($"To: {emailSettings.Value.ToEmail}");
        emailBuilder.AppendLine("Subject: Weekly Restaurant Reports");
        emailBuilder.AppendLine($"MIME-Version: 1.0");
        emailBuilder.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
        emailBuilder.AppendLine();
        
        // Email body
        emailBuilder.AppendLine($"--{boundary}");
        emailBuilder.AppendLine("Content-Type: text/plain; charset=UTF-8");
        emailBuilder.AppendLine();
        emailBuilder.AppendLine("Please find the attached weekly restaurant reports.");
        emailBuilder.AppendLine();
        
        // Add each attachment
        foreach (var (content, fileName, mimeType) in attachments)
        {
            emailBuilder.AppendLine($"--{boundary}");
            emailBuilder.AppendLine($"Content-Type: {mimeType}; name=\"{fileName}\"");
            emailBuilder.AppendLine("Content-Disposition: attachment; filename=\"" + fileName + "\"");
            emailBuilder.AppendLine("Content-Transfer-Encoding: base64");
            emailBuilder.AppendLine();
            
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
}
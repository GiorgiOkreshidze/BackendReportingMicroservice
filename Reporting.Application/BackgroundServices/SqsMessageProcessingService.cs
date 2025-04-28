using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reporting.Application.DTOs;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Repositories.Interfaces;

namespace Reporting.Application.BackgroundServices
{
    public class SqsMessageProcessingService(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IOptions<AwsSettings> awsSettings,
        ILogger<SqsMessageProcessingService> logger) : BackgroundService
    {
        private const string FixedPartitionKey = "weekly";
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("SQS message processing service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var receiveMessageRequest = new ReceiveMessageRequest
                    {
                        QueueUrl = awsSettings.Value.SqsQueueUrl,
                        MaxNumberOfMessages = 10,
                        WaitTimeSeconds = 20
                    };
                    var response = await sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);
                    if (response.Messages.Count > 0)
                    {
                        logger.LogInformation("Received {MessageId} messages from SQS", response.Messages.Count);
                        foreach (var message in response.Messages)
                        {
                            try
                            {
                                // Process the message
                                await ProcessMessageAsync(message, stoppingToken);

                                // Delete the message from the queue
                                await sqsClient.DeleteMessageAsync(awsSettings.Value.SqsQueueUrl, message.ReceiptHandle, stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex,"Error processing message {MessageId}", message.MessageId);
                                // Consider implementing a dead-letter queue for failed messages
                            }
                        }
                    }
                    else
                    {
                        logger.LogInformation("No messages received");
                        await Task.Delay(1000, stoppingToken); // Add small delay to prevent CPU spinning
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Error in SQS message processing service");
                    // Wait before retrying to avoid high CPU usage in case of persistent errors
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }

            private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
        {
            logger.LogInformation("Processing message: {MessageId}", message.MessageId);
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                // Deserialize the message body to ReportDto
                var messageContent = JsonSerializer.Deserialize<SqsEventMessage>(message.Body, options);

                if (messageContent == null)
                {
                    logger.LogWarning("Failed to deserialize message {MessageId}", message.MessageId);
                    
                    return;
                }
                var reportDto = messageContent.payload.Deserialize<ReportDto>();
                if (reportDto == null)
                {
                    logger.LogWarning("Failed to convert payload to ReportDto for message {MessageId}", message.MessageId);
                    return;
                }
                // Process the report using a scoped service
                using (var scope = serviceProvider.CreateScope())
                {

                    var reportRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();
                    var id = Guid.NewGuid().ToString();
                    var reportEntity = new Report
                    {
                        Partition = FixedPartitionKey,
                        DateId = $"{reportDto.Date}#{id}",
                        Id = id,
                        Date = reportDto.Date,
                        Location = reportDto.Location,
                        Waiter = reportDto.Waiter,
                        WaiterEmail = reportDto.WaiterEmail,
                        HoursWorked = reportDto.HoursWorked,
                        AverageServiceFeedback = reportDto.AverageServiceFeedback,
                        MinimumServiceFeedback = reportDto.MinimumServiceFeedback,
                    };
                    await reportRepository.SaveReportAsync(reportEntity, cancellationToken);
                }
                logger.LogInformation("Successfully processed message {MessageId}", message.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize message {MessageId}", message.MessageId);
                throw;
            }
        }
    }
}

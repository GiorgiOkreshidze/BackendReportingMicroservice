using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Reporting.Application.DTOs;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.RabbitMq;
using Reporting.Infrastructure.Repositories.Interfaces;

namespace Reporting.Application.BackgroundServices;

public class RabbitMqMessageProcessingService(
    IServiceProvider serviceProvider,
    IOptions<RabbitMqSettings> rabbitMqSettings,
    ILogger<RabbitMqMessageProcessingService> logger) : BackgroundService
{
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RabbitMQ message processing service is starting");

        var factory = new ConnectionFactory
        {
            HostName = rabbitMqSettings.Value.HostName,
            Port = rabbitMqSettings.Value.Port,
            UserName = rabbitMqSettings.Value.UserName,
            Password = rabbitMqSettings.Value.Password
        };

        try
        {
            var connection = await factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue: "report-events", durable: true, exclusive: false, autoDelete: false, arguments: null);
            
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                logger.LogInformation("Received message: {Message}", message);

                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var messageContent = JsonSerializer.Deserialize<SqsEventMessage>(message, options);

                    using var scope = serviceProvider.CreateScope();
                    var reportRepository = scope.ServiceProvider.GetRequiredService<IReportRepository>();

                    var reportDto = messageContent.payload.Deserialize<ReportDto>();
                    var id = Guid.NewGuid().ToString();
                    var reportEntity = new Report
                    {
                        Id = id,
                        DateId = $"{reportDto.Date}#{id}",
                        Date = reportDto.Date,
                        Location = reportDto.Location,
                        LocationId = reportDto.LocationId,
                        Waiter = reportDto.Waiter,
                        WaiterEmail = reportDto.WaiterEmail,
                        HoursWorked = reportDto.HoursWorked,
                        OrderId = reportDto.OrderId,
                        OrderRevenue = reportDto.OrderRevenue,
                        AverageServiceFeedback = reportDto.AverageServiceFeedback,
                        MinimumServiceFeedback = reportDto.MinimumServiceFeedback,
                        AverageCuisineFeedback = reportDto.AverageCuisineFeedback,
                        MinimumCuisineFeedback = reportDto.MinimumCuisineFeedback
                    };
                    await reportRepository.SaveReportAsync(reportEntity, stoppingToken);
                    logger.LogInformation("Processed message");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing RabbitMQ message");
                }
            };

            await channel.BasicConsumeAsync(queue: "report-events", autoAck: true, consumer: consumer);
            
            // Keep the service running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Error in RabbitMQ message processing service");
            throw;
        }
    }
}
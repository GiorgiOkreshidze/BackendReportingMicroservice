using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Reporting.Application.Interfaces;

namespace Reporting.Application.BackgroundServices;

public class ReportSenderBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ReportSenderBackgroundService> logger)
    : BackgroundService
{
    //TESTING -> Report will be generated every minute
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ReportSenderBackgroundService is starting.");

        /*while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IReportServiceSender>();
            try
            {
                // Calculate the next run time (e.g., 10 seconds from now for testing)
                var now = DateTime.UtcNow;
                var nextRun = now.AddMinutes(1);
                var delay = nextRun - now;

                if (delay.TotalMilliseconds > 0)
                {
                    logger.LogInformation("Waiting until {NextRun} to send report.", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }

                logger.LogInformation("Sending weekly report at {Time}.", DateTime.UtcNow);
                await  reportService.SendReportEmailAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while sending report.");
            }
        }*/

        logger.LogInformation("ReportSenderBackgroundService is stopping.");
    }

    //ONE WEEK -> Report will be generated every Monday at midnight

    /*
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ReportSenderBackgroundService is starting.");
    
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IReportServiceSender>();
            
            try
            {
                // Calculate the next run time (e.g., next Monday at midnight)
                var now = DateTime.UtcNow;
                var nextRun = GetNextMondayMidnight(now);
                var delay = nextRun - now;
    
                if (delay.TotalMilliseconds > 0)
                {
                    logger.LogInformation("Waiting until {NextRun} to send report.", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }
    
                logger.LogInformation("Sending weekly report at {Time}.", DateTime.UtcNow);
                await reportService.SendReportEmailAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while sending report.");
            }
        }
    
        logger.LogInformation("ReportSenderBackgroundService is stopping.");
    }
    */

    private static DateTime GetNextMondayMidnight(DateTime now)
    {
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0 && now.TimeOfDay > TimeSpan.Zero)
        {
            daysUntilMonday = 7; // If today is Monday and time is past midnight, schedule for next Monday
        }
        return now.Date.AddDays(daysUntilMonday);
    }
}
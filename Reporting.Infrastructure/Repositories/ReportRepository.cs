using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Logging;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Exceptions;
using Reporting.Infrastructure.Repositories.Interfaces;

namespace Reporting.Infrastructure.Repositories;

public class ReportRepository(IDynamoDBContext context, ILogger<ReportRepository> logger) : IReportRepository
{
    private const string FixedPartitionKey = "weekly";

    public async Task<List<Report>> RetrieveReports(DateTime startDate, DateTime endDate)
    {
        try
        {
            logger.LogInformation("Retrieving reports from {StartDate} to {EndDate}", startDate, endDate);

            var config = new QueryOperationConfig
            {
                Filter = new QueryFilter(),
            };

            config.Filter.AddCondition("partition", QueryOperator.Equal, FixedPartitionKey);
            config.Filter.AddCondition("date#id", QueryOperator.Between,
                startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd") + "#z");

            var reports = await context.FromQueryAsync<Report>(config).GetRemainingAsync();
            return reports;
        }
        catch (AmazonDynamoDBException ex)
        {
            logger.LogError(ex, "Failed to retrieve reports from DynamoDB");
            throw new ReportRetrievalException("An error occurred while retrieving reports from DynamoDB.", ex);
        }
    }

    public async Task SaveReportAsync(Report report, CancellationToken cancellationToken = default)
    {
        try 
        { 
            await context.SaveAsync(report, cancellationToken);
            logger.LogInformation("Successfully saved report to DynamoDB for waiter {Waiter}", report.Waiter);
        }
        catch (AmazonDynamoDBException ex)
        {
            logger.LogError(ex, "DynamoDB-specific error occurred while saving report for waiter {Waiter}", report.Waiter);
            throw new ReportSaveException("An error occurred while saving the report to DynamoDB.", ex);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex,"Save operation was canceled for waiter {Waiter}", report.Waiter);
            throw;
        }
        catch (Exception ex) 
        {
            logger.LogError(ex, "Unexpected error saving report to DynamoDB for waiter {Waiter}", report.Waiter);
            throw new ReportSaveException("An unexpected error occurred while saving the report.", ex);
        }
    }
}

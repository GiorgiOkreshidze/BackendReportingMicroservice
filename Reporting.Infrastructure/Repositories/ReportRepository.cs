using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Exceptions;
using Reporting.Infrastructure.Repositories.Interfaces;

namespace Reporting.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private const string FixedPartitionKey = "weekly";
    private readonly IMongoCollection<Report> _collection;
    private readonly ILogger<ReportRepository> _logger;

    public ReportRepository(IMongoDatabase database, ILogger<ReportRepository> logger)
    {
        _collection = database.GetCollection<Report>("Reports");
        _logger = logger;

        CreateIndexes();
    }

    private void CreateIndexes()
    {
        _collection.Indexes.CreateOne(new CreateIndexModel<Report>(Builders<Report>.IndexKeys.Ascending(r => r.DateId)));
        _collection.Indexes.CreateOne(new CreateIndexModel<Report>(Builders<Report>.IndexKeys.Ascending(r => r.LocationId)));
        _collection.Indexes.CreateOne(new CreateIndexModel<Report>(Builders<Report>.IndexKeys.Ascending(r => r.Date)));
    }
    
    public async Task<List<Report>> RetrieveReports(DateTime startDate, DateTime endDate)
    {
        try
        {
            _logger.LogInformation("Retrieving reports from {StartDate} to {EndDate}", startDate, endDate);

            var filter = Builders<Report>.Filter.And(
                Builders<Report>.Filter.Eq(r => r.Partition, FixedPartitionKey),
                Builders<Report>.Filter.Gte(r => r.DateId, startDate.ToString("yyyy-MM-dd")),
                Builders<Report>.Filter.Lte(r => r.DateId, endDate.ToString("yyyy-MM-dd") + "#z")
            );

            var reports = await _collection.Find(filter).ToListAsync();
            return reports;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to retrieve reports from MongoDB");
            throw new ReportRetrievalException("An error occurred while retrieving reports from MongoDB.", ex);
        }
    }
    
    public async Task<IEnumerable<Report>> RetrieveReportsForAdmin(DateTime startDate, DateTime endDate, string? locationId)
    {
        try
        {
            _logger.LogInformation("Retrieving reports from {StartDate} to {EndDate}{LocationFilter}",
                startDate,
                endDate,
                locationId != null ? $" for location '{locationId}'" : "");

            var filterBuilder = Builders<Report>.Filter;
            var filters = new List<FilterDefinition<Report>>
            {
                filterBuilder.Eq(r => r.Partition, FixedPartitionKey),
                filterBuilder.Gte(r => r.DateId, startDate.ToString("yyyy-MM-dd")),
                filterBuilder.Lte(r => r.DateId, endDate.ToString("yyyy-MM-dd") + "#z")
            };

            if (!string.IsNullOrEmpty(locationId))
            {
                filters.Add(filterBuilder.Eq(r => r.LocationId, locationId));
            }

            var filter = filterBuilder.And(filters);
            var reports = await _collection.Find(filter).ToListAsync();
            return reports;
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "Failed to retrieve reports from MongoDB");
            throw new ReportRetrievalException("An error occurred while retrieving reports from MongoDB.", ex);
        }
    }
    
    public async Task SaveReportAsync(Report report, CancellationToken cancellationToken = default)
    {
        try
        {
            await _collection.InsertOneAsync(report, null, cancellationToken);
            _logger.LogInformation("Successfully saved report to MongoDB for waiter {Waiter}", report.Waiter);
        }
        catch (MongoException ex)
        {
            _logger.LogError(ex, "MongoDB-specific error occurred while saving report for waiter {Waiter}", report.Waiter);
            throw new ReportSaveException("An error occurred while saving the report to MongoDB.", ex);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Save operation was canceled for waiter {Waiter}", report.Waiter);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving report to MongoDB for waiter {Waiter}", report.Waiter);
            throw new ReportSaveException("An unexpected error occurred while saving the report.", ex);
        }
    }
}

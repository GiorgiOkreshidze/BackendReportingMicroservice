using Amazon.DynamoDBv2.DataModel;

namespace Reporting.Domain.Entities;

[DynamoDBTable("Reports")]
public class Report
{
    [DynamoDBHashKey("partition")]
    public required string Partition { get; set; } = "weekly";

    [DynamoDBRangeKey("date#id")]
    public required string DateId { get; set; }

    [DynamoDBProperty("id")]
    public required string Id { get; set; }

    [DynamoDBProperty("location")]
    public required string Location { get; set; }
    
    [DynamoDBProperty("locationId")]
    public required string LocationId { get; set; }
        
    [DynamoDBProperty("date")]
    public required string Date { get; set; }
        
    [DynamoDBProperty("waiter")]
    public required string Waiter { get; set; }
        
    [DynamoDBProperty("waiterEmail")]
    public required string WaiterEmail { get; set; }
        
    [DynamoDBProperty("hoursWorked")]
    public required double HoursWorked { get; set; }
    
    [DynamoDBProperty("averageServiceFeedback")]
    public required double AverageServiceFeedback { get; set; }

    [DynamoDBProperty("minimumServiceFeedback")]
    public required int MinimumServiceFeedback { get; set; }
}
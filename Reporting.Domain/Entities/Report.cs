using MongoDB.Bson.Serialization.Attributes;

namespace Reporting.Domain.Entities;

public class Report
{
    [BsonId]
    public required string Id { get; set; }
    
    public required string Partition { get; set; } = "weekly";

    public required string DateId { get; set; }

    public required string Location { get; set; }
    
    public required string LocationId { get; set; }
        
    public required string Date { get; set; }
        
    public required string Waiter { get; set; }
        
    public required string WaiterEmail { get; set; }
        
    public required double HoursWorked { get; set; }
    
    public required double AverageServiceFeedback { get; set; }

    public required int MinimumServiceFeedback { get; set; }
}
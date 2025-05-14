using MongoDB.Bson.Serialization.Attributes;
using Reporting.Domain.Shared;

namespace Reporting.Domain.Entities;

public class Report : ReportBase
{
    [BsonId]
    public required string Id { get; set; }

    public required string DateId { get; set; }
}
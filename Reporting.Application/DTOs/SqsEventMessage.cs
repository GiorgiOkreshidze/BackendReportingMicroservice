using System.Text.Json;

namespace Reporting.Application.DTOs
{
    public class SqsEventMessage
    {
        public string eventType { get; set; }
        
        public JsonElement payload { get; set; }
    }
}

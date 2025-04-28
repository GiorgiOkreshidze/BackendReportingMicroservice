namespace Reporting.Application.DTOs
{
    public class ReportDto
    {
        public required string Location { get; set; }

        public required string Date { get; set; }

        public required string Waiter { get; set; }

        public required string WaiterEmail { get; set; }

        public required int HoursWorked { get; set; }

        public required double AverageServiceFeedback { get; set; }

        public required int MinimumServiceFeedback { get; set; }
    }
}

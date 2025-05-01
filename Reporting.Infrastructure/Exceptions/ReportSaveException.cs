namespace Reporting.Infrastructure.Exceptions;
public class ReportSaveException : Exception
{
    public ReportSaveException() { }

    public ReportSaveException(string message) : base(message) { }
    
    public ReportSaveException(string message, Exception innerException) : base(message, innerException) { }
}

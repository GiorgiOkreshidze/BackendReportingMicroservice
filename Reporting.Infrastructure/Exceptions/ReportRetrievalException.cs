namespace Reporting.Infrastructure.Exceptions;

public class ReportRetrievalException(string message, Exception innerException) : Exception(message, innerException);
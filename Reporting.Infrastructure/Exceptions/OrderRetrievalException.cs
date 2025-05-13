namespace Reporting.Infrastructure.Exceptions;

public class OrderRetrievalException(string message, Exception innerException) : Exception(message, innerException);
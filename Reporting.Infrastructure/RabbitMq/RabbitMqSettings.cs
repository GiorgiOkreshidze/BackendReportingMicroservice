namespace Reporting.Infrastructure.RabbitMq;

public class RabbitMqSettings
{
    public string HostName { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
    
    public int Port { get; set; }
}
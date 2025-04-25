using Amazon.SimpleEmail;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Application.Formatters;
using Reporting.Application.Formatters.Interfaces;
using Reporting.Application.Interfaces;

namespace Reporting.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<AmazonSimpleEmailServiceClient>();
        
        services.AddScoped<IReportServiceSender, ReportSenderService>();
        services.AddScoped<IReportGenerator, ExcelReportGenerator>();
        services.AddScoped<IEmailSenderService, EmailSenderService>();
        services.AddScoped<IReportProcessService, ReportProcessService>();
        
        return services;
    }
}
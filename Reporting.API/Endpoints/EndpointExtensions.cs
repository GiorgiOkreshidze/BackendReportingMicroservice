namespace Reporting.API.Endpoints;

public static class EndpointExtensions
{
    public static WebApplication MapReportEndpoints(this WebApplication app)
    {
        ReportEndpoints.MapEndpoints(app);
        DownloadEndpoints.MapEndpoints(app);
        
        return app;
    }
}
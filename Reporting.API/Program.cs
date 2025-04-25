using Microsoft.OpenApi.Models;
using Reporting.Application;
using Reporting.Application.BackgroundServices;
using Reporting.Application.DTOs.Emails;
using Reporting.Application.Interfaces;
using Reporting.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Restaurant Api",
        Description = "A Web API for managing restaurant operations",
    });
});

builder.Services.Configure<EmailSettings>(options => {
    options.FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL") ?? throw new ArgumentNullException(nameof(EmailSettings.FromEmail), "FROM_EMAIL environment variable is not set");
    options.ToEmail = Environment.GetEnvironmentVariable("TO_EMAIL") ?? throw new ArgumentNullException(nameof(EmailSettings.ToEmail), "TO_EMAIL environment variable is not set");
});

builder.Services.AddHostedService<ReportSenderBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Hello World!");
app.MapPost("/send-report", async (IReportServiceSender reportSenderService, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Received request to send report via /send-report endpoint.");
        await reportSenderService.SendReportEmailAsync();
        return Results.Ok("Report sent successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to send report via /send-report endpoint.");
        return Results.Problem("Failed to send report.", statusCode: 500);
    }
});

app.UseHttpsRedirection();

app.Run();
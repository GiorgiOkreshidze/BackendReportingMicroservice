using Microsoft.OpenApi.Models;
using Reporting.API.Endpoints;
using Reporting.Application;
using Reporting.Application.BackgroundServices;
using Reporting.Application.DTOs;
using Reporting.Application.DTOs.Emails;
using Reporting.Infrastructure;
using Reporting.Infrastructure.RabbitMq;

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
        Title = "Restaurant Reporting Service Api",
        Description = "A Web API for managing restaurant reporting operations",
    });
});

builder.Services.Configure<EmailSettings>(options => {
    options.FromEmail = Environment.GetEnvironmentVariable("FROM_EMAIL") ?? throw new ArgumentNullException(nameof(EmailSettings.FromEmail), "FROM_EMAIL environment variable is not set");
    options.ToEmail = Environment.GetEnvironmentVariable("TO_EMAIL") ?? throw new ArgumentNullException(nameof(EmailSettings.ToEmail), "TO_EMAIL environment variable is not set");
});

builder.Services.Configure<AwsSettings>(options => {
    options.SqsQueueUrl = Environment.GetEnvironmentVariable("SQS_QUEUE_URL") ?? throw new ArgumentNullException(nameof(AwsSettings.SqsQueueUrl), "SQS_QUEUE_URL environment variable is not set");
});

builder.Services.Configure<RabbitMqSettings>(options => {
    options.HostName = Environment.GetEnvironmentVariable("HOST_NAME") ?? throw new ArgumentNullException(nameof(RabbitMqSettings.HostName), "HOST_NAME environment variable is not set");
    options.UserName = Environment.GetEnvironmentVariable("RABBIT_MQ_USERNAME") ?? throw new ArgumentNullException(nameof(RabbitMqSettings.UserName), "RABBIT_MQ_USERNAME environment variable is not set");
    options.Password = Environment.GetEnvironmentVariable("RABBIT_MQ_PASSWORD") ?? throw new ArgumentNullException(nameof(RabbitMqSettings.Password), "RABBIT_MQ_PASSWORD environment variable is not set");
    
    if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int port))
        options.Port = port;
});

builder.Services.AddHostedService<ReportSenderBackgroundService>();
builder.Services.AddHostedService<RabbitMqMessageProcessingService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("http://localhost:8080","http://restaurants-run7team2-api-handler-dev.development.krci-dev.cloudmentor.academy", "https://restaurants-run7team2-api-handler-dev.development.krci-dev.cloudmentor.academy") // Allow requests from this origin
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// lets check
app.UseHttpsRedirection();
app.UseCors();

app.MapGet("/", () => "Hello world!");
app.MapReportEndpoints();
app.Run();


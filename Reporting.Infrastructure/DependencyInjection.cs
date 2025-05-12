using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SimpleEmail;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reporting.Infrastructure.AWS;
using Reporting.Infrastructure.MongoDB;
using Reporting.Infrastructure.Repositories;
using Reporting.Infrastructure.Repositories.Interfaces;
using MongoDB.Driver;

namespace Reporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var credentialsFactory = new AwsCredentialsFactory(configuration);

        // Use .Result to block until the async method completes, Injection should be done in a non-async context.
        var (stsClient, credentials) = credentialsFactory.CreateCredentialsAsync().Result;

        services.AddSingleton(stsClient);
        services.AddSingleton(credentials);

        services.AddSingleton<IAmazonDynamoDB>(_ => DynamoDbFactory.CreateDynamoDbClient(credentials));
        services.AddSingleton<IDynamoDBContext>(sp =>
            DynamoDbFactory.CreateDynamoDbContext(sp.GetRequiredService<IAmazonDynamoDB>()));
        
        services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING_STANDARD_SRV") ?? 
                                       Environment.GetEnvironmentVariable("CONNECTION_STRING_STANDARD") ?? 
                                       "mongodb://localhost:27017";
            options.DatabaseName = "RestaurantDb";
        });
        
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return client.GetDatabase(settings.DatabaseName);
        });

        services.AddSingleton<IAmazonSQS>(_ => SqsFactory.CreateSqsClient(credentials));
        services.AddSingleton<IAmazonSimpleEmailService>(_ => SesFactory.CreateSesClient(credentials));
        
        services.AddScoped<IReportRepository, ReportRepository>();

        return services;
    }
}
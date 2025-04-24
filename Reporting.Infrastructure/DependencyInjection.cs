using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Infrastructure.AWS;

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

        services.AddSingleton<IAmazonSQS>(_ => SqsFactory.CreateSqsClient(credentials));
        return services;
    }
}
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;

namespace Reporting.Infrastructure.AWS;

public static class DynamoDbFactory
{
    public static IAmazonDynamoDB CreateDynamoDbClient(AWSCredentials credentials)
    {
        var creds = credentials.GetCredentials();
        Console.WriteLine($"Creating DynamoDB client with AccessKeyId: {creds.AccessKey[..5]}...");

        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = RegionEndpoint.EUWest2
        };

        var sessionCredentials = new SessionAWSCredentials(
            creds.AccessKey,
            creds.SecretKey,
            creds.Token);

        return new AmazonDynamoDBClient(sessionCredentials, config);
    }

    public static IDynamoDBContext CreateDynamoDbContext(IAmazonDynamoDB client)
    {
        var context = new DynamoDBContext(client);
        Console.WriteLine("DynamoDB context created with assumed role credentials");
        return context;
    } 
}
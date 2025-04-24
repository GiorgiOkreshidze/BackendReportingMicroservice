using Amazon;
using Amazon.Runtime;
using Amazon.SQS;

namespace Reporting.Infrastructure.AWS;

public static class SqsFactory
{
    public static IAmazonSQS CreateSqsClient(AWSCredentials credentials)
    {
        var creds = credentials.GetCredentials();
        Console.WriteLine($"Creating Sqs client with AccessKeyId: {creds.AccessKey[..5]}...");

        var config = new AmazonSQSConfig
        {
            RegionEndpoint = RegionEndpoint.EUWest2
        };

        var sessionCredentials = new SessionAWSCredentials(
            creds.AccessKey,
            creds.SecretKey,
            creds.Token);

        return new AmazonSQSClient(sessionCredentials, config);
    }
}
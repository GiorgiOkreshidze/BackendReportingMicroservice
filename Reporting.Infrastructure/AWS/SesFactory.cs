using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;

namespace Reporting.Infrastructure.AWS;

public static class SesFactory
{
    public static IAmazonSimpleEmailService CreateSesClient(AWSCredentials credentials)
    {
        var creds = credentials.GetCredentials();
        Console.WriteLine($"Creating Ses client with AccessKeyId: {creds.AccessKey[..5]}...");

        var config = new AmazonSimpleEmailServiceConfig
        {
            RegionEndpoint = RegionEndpoint.EUWest2
        };

        var sessionCredentials = new SessionAWSCredentials(
            creds.AccessKey,
            creds.SecretKey,
            creds.Token);

        return new AmazonSimpleEmailServiceClient(sessionCredentials, config);
    }
}
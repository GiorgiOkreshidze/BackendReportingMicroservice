using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.Configuration;

namespace Reporting.Infrastructure.AWS;

public class AwsCredentialsFactory(IConfiguration configuration)
{
    public async Task<(IAmazonSecurityTokenService StsClient, AWSCredentials Credentials)> CreateCredentialsAsync()
    {
        var awsConfig = configuration.GetSection("AWS");
        var profileName = awsConfig["Profile"] ?? "default";

        var chain = new CredentialProfileStoreChain();
        AmazonSecurityTokenServiceClient stsClient;

        if (chain.TryGetAWSCredentials(profileName, out var baseCredentials))
        {
            stsClient = new AmazonSecurityTokenServiceClient(baseCredentials, RegionEndpoint.EUWest2);
        }
        else
        {
            Console.WriteLine($"Could not load AWS profile '{profileName}', falling back to default.");
            stsClient = new AmazonSecurityTokenServiceClient(RegionEndpoint.EUWest2);
        }

        var callerIdRequest = new GetCallerIdentityRequest();
        var caller = await stsClient.GetCallerIdentityAsync(callerIdRequest);
        Console.WriteLine($"Original Caller: {caller.Arn}");

        var developerRoleArn = awsConfig["DeveloperRoleArn"] ??
                               "arn:aws:iam::761018851832:role/org/DeveloperAccessRoleTeam2";
        var roleSessionName = awsConfig["RoleSessionName"] ?? "k8sSession";

        var autoRefreshingCredentials = new AutoRefreshingRoleCredentials(
            stsClient,
            developerRoleArn,
            roleSessionName);

        var tempClient = new AmazonSecurityTokenServiceClient(autoRefreshingCredentials, RegionEndpoint.EUWest2);
        var assumedCaller = await tempClient.GetCallerIdentityAsync(new GetCallerIdentityRequest());
        Console.WriteLine($"AssumedRole Caller: {assumedCaller.Arn}");

        return (stsClient, autoRefreshingCredentials);
    }
}
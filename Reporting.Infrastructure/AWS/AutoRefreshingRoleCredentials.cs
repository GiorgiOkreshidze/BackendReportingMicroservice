using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace Reporting.Infrastructure.AWS;

public class AutoRefreshingRoleCredentials : AWSCredentials
{
    private readonly string _roleArn;
    private readonly string _roleSessionName;
    private readonly IAmazonSecurityTokenService _stsClient;
    private readonly int _durationSeconds;
    private readonly TimeSpan _refreshWindow;
     
    private Credentials? _currentCredentials;
    private DateTime _expirationTime;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
     
    public AutoRefreshingRoleCredentials(
        IAmazonSecurityTokenService stsClient,
        string roleArn,
        string roleSessionName,
        int durationSeconds = 3600,
        TimeSpan? refreshWindow = null)
    {
        _stsClient = stsClient;
        _roleArn = roleArn;
        _roleSessionName = roleSessionName;
        _durationSeconds = durationSeconds;
        _refreshWindow = refreshWindow ?? TimeSpan.FromMinutes(5);
         
        // Initialize credentials immediately
        RefreshCredentialsAsync().GetAwaiter().GetResult();
    }
    public override ImmutableCredentials GetCredentials()
    {
        if (_currentCredentials == null || DateTime.UtcNow >= _expirationTime.Subtract(_refreshWindow))
        {
            Console.WriteLine("Refreshing credentials in GetCredentials()");
            RefreshCredentialsAsync().GetAwaiter().GetResult();
        }
        else
        {
            Console.WriteLine("Using existing credentials that expire at: " + _expirationTime);
        }
    
        Console.WriteLine($"Returning credentials with AccessKeyId: {_currentCredentials!.AccessKeyId.Substring(0, 5)}...");
    
        return new ImmutableCredentials(
            _currentCredentials.AccessKeyId,
            _currentCredentials.SecretAccessKey,
            _currentCredentials.SessionToken);
    }
    
    private async Task RefreshCredentialsAsync()
    {
        // Use semaphore to prevent multiple simultaneous refreshes
        await _semaphore.WaitAsync();
        try
        {
            // Double-check if refresh is still needed after acquiring the lock
            if (_currentCredentials == null || DateTime.UtcNow >= _expirationTime.Subtract(_refreshWindow))
            {
                var request = new AssumeRoleRequest
                {
                    RoleArn = _roleArn,
                    RoleSessionName = _roleSessionName,
                    DurationSeconds = _durationSeconds
                };
                
                var response = await _stsClient.AssumeRoleAsync(request);
                _currentCredentials = response.Credentials;
                _expirationTime = response.Credentials.Expiration;
                
                Console.WriteLine($"Credentials refreshed, new expiration: {_expirationTime}");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
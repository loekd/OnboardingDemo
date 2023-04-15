using Microsoft.Extensions.Options;
using IdentityModel.Client;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http.Headers;
using ExternalScreening.Api.Services;
using Microsoft.Identity.Client;

namespace ExternalScreening.Api;

/// <summary>
/// Helper that acquires an Identity Server access token, when calling the External Screening Api.
/// It uses the client_credentials flow to fetch and cache tokens.
/// The injected HttpClient is pre-configured with the IDP endpoint.
/// </summary>
public class CustomAuthorizationMessageHandler : DelegatingHandler
{
    private readonly HttpClient _idpHttpClient;
    private readonly IDistributedCache _cache;
    private readonly OnboardingApiOptions _configuration;
    private readonly ILogger<CustomAuthorizationMessageHandler> _logger;
    private const string TOKEN_CACHE_KEY = "Access_Token";

    public CustomAuthorizationMessageHandler(HttpClient idpHttpClient, IDistributedCache cache, IOptions<OnboardingApiOptions> configuration, ILogger<CustomAuthorizationMessageHandler> logger)
    {
        _idpHttpClient = idpHttpClient ?? throw new ArgumentNullException(nameof(idpHttpClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? token = _cache.GetString(TOKEN_CACHE_KEY);
        if (token is null)
        {
            //fetch new token
            DiscoveryDocumentResponse disco = await FetchDiscoveryDocument(cancellationToken);
            var tokenResponse = await GetAccessToken(disco, cancellationToken);
            string identityServerToken = tokenResponse.AccessToken;

            //exchange Identity Server token for Azure AD token

            var client = ConfidentialClientApplicationBuilder.Create(_configuration.AzureAdClientId)
                .WithClientAssertion(identityServerToken)
                .WithAuthority(_configuration.AzureAdAuthority)
                .Build();

            var query = client.AcquireTokenForClient(new[] { _configuration.AzureAdRequestedScope });
            var response = await query.ExecuteAsync();
            if (string.IsNullOrEmpty(response?.AccessToken))
            {
                throw new InvalidOperationException("Failed to fetch token for Azure AD");
            }

            token = response.AccessToken;

            //cache it while it's valid
            _cache.SetString(TOKEN_CACHE_KEY, token, options: new DistributedCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 120) });
        }
        //send access token as bearer token
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Uses the fetched discovery data to get an access token from the IDP token endpoint.
    /// </summary>
    /// <param name="disco"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async ValueTask<TokenResponse> GetAccessToken(DiscoveryDocumentResponse disco, CancellationToken cancellationToken)
    {
        var tokenResponse = await _idpHttpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = _configuration.IdentityServerClientId,
            ClientSecret = _configuration.IdentityServerClientSecret,
            Scope = _configuration.IdentityServerRequestedReadWriteScope,
        }, cancellationToken);

        if (tokenResponse.IsError)
        {
            _logger.LogCritical("Failed to acquire token. {Error} - {Description}.", tokenResponse.Error, tokenResponse.ErrorDescription);
            throw new InvalidOperationException("Failed to authenticate with Identity Server");
        }
        return tokenResponse;
    }

    /// <summary>
    /// Fetches IDP metadata from public endpoint.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<DiscoveryDocumentResponse> FetchDiscoveryDocument(CancellationToken cancellationToken)
    {
        var disco = await _idpHttpClient.GetDiscoveryDocumentAsync(cancellationToken: cancellationToken);
        if (disco.IsError)
        {
            _logger.LogCritical("Failed to fetch discovery document from Identity Server. {Error}", disco.Error);
            throw new InvalidOperationException("Failed to fetch discovery document from Identity Server");
        }
        return disco;
    }
}

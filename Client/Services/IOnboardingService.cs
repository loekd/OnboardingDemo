using Onboarding.Shared;
using System.Net.Http.Json;
using System.Text;

namespace Onboarding.Client.Services;

public interface IOnboardingService
{
    /// <summary>
    /// Gets all registered onboardings
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<OnboardingModel>> GetAll();

    /// <summary>
    /// Adds one new onboarding to the set
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task Add(CreateOnboardingModel model);

}

public class OnboardingService : IOnboardingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OnboardingService> _logger;
    private const string _apiEndpoint = "api/onboardings";

    public OnboardingService(HttpClient httpClient, ILogger<OnboardingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<OnboardingModel>> GetAll()
    {
        _logger.LogDebug("Fetching all onboarding data");
        var result = await _httpClient.GetFromJsonAsync<OnboardingModel[]>(_apiEndpoint);
        return result ?? Array.Empty<OnboardingModel>();
    }

    public async Task Add(CreateOnboardingModel model)
    {
        _logger.LogDebug("Adding new onboarding data");
        var result = await _httpClient.PostAsJsonAsync(_apiEndpoint, model);
        result.EnsureSuccessStatusCode();
    }

    public static Task<HttpResponseMessage> FallbackGetOnboardings(Polly.Context _, CancellationToken __)
    {
        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }


}

using ExternalScreening.Api.Models;

namespace ExternalScreening.Api.Services;

public interface IOnboardingService
{
    Task SendScreeningResultAsync(ScreeningEntity screening);
}

/// <summary>
/// Uses federated credentials to call the onboarding API.
/// </summary>
public class OnboardingService : IOnboardingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OnboardingService> _logger;
    private const string _apiEndpoint = "api/onboardings/{0}/screenings/{1}";

    public OnboardingService(HttpClient httpClient, ILogger<OnboardingService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendScreeningResultAsync(ScreeningEntity screening)
    {
        _logger.LogTrace("Posting screening result for onboarding {OnboardingId} with screening {ScreeningID}.", screening.OnboardingId, screening.Id);

        string requestUri = string.Format(_apiEndpoint, screening.OnboardingId, screening.Id);
        var result = new ScreeningResult(screening.Id, screening.FirstName!, screening.LastName!, screening.IsApproved, screening.OnboardingId);
        var response = await _httpClient.PostAsJsonAsync(requestUri, result);
        response.EnsureSuccessStatusCode();
    }
}

﻿using System.ComponentModel.DataAnnotations;

namespace Onboarding.Server.Services
{
    public record CreateScreeningRequest([Required] string FirstName, [Required] string LastName, [Required] Guid OnboardingId);

    public record CreateScreeningResponse(Guid ScreeningId, string FirstName, string LastName);

    public record ScreeningResult([Required] string FirstName, [Required] string LastName, bool? IsApproved);

    public interface IExternalScreeningService
    {
        Task<CreateScreeningResponse> RequestScreening(CreateScreeningRequest request);
    }

    public class ExternalScreeningService : IExternalScreeningService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalScreeningService> _logger;
        private const string _apiEndpoint = "api/screenings";


        public ExternalScreeningService(HttpClient httpClient, ILogger<ExternalScreeningService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CreateScreeningResponse> RequestScreening(CreateScreeningRequest request)
        {
            _logger.LogTrace("Requesting screening");

            var response = await _httpClient.PostAsJsonAsync(_apiEndpoint, request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CreateScreeningResponse>();
            return result!;
        }
    }
}

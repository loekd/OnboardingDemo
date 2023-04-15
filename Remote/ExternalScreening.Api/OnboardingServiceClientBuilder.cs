using ExternalScreening.Api.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace ExternalScreening.Api;

public static class OnboardingServiceClientBuilder
{
    public static WebApplicationBuilder ConfigureOnboardingService(this WebApplicationBuilder builder)
    {
        // Add services to the container.
        builder.Services
            .AddOptions<OnboardingApiOptions>()
            .BindConfiguration(OnboardingApiOptions.ConfigurationSectionName)
            .ValidateDataAnnotations();

        var onboardingConfig = builder.Configuration
            .GetRequiredSection(OnboardingApiOptions.ConfigurationSectionName)
            .Get<OnboardingApiOptions>()!;
        Console.WriteLine($"Onboarding API Config: {onboardingConfig}");

        builder
            .ConfigureScreeningIdpClient(onboardingConfig)
            .ConfigureOnboardingApiClient(onboardingConfig);

        builder.Services.AddScoped<IOnboardingService>(sp =>
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Onboarding.API");
            var logger = sp.GetRequiredService<ILogger<OnboardingService>>();
            return new OnboardingService(client, logger);
        });
        return builder;
    }

    /// <summary>
    /// Configures a named HttpClient to talk to the Screening API's Identity Provider, used to get access tokens for the Onboarding API by exchanging the Identity Server
    /// token for an Azure AD token using Federated Credentials.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static WebApplicationBuilder ConfigureScreeningIdpClient(this WebApplicationBuilder builder, OnboardingApiOptions config)
    {
        builder.Services.AddHttpClient<CustomAuthorizationMessageHandler>("Screening.Idp", client => client.BaseAddress = new Uri(config.Authority))
            .AddPolicyHandler((sp, msg) => PolicyBuilder.GetRetryPolicy<CustomAuthorizationMessageHandler>(sp, 5))  //retry 5 times
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (builder.Environment.IsDevelopment())
                {
                    //Disable certificate checking for IDP in DEV
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                }
                return handler;
            });

        builder.Services.AddScoped<IOnboardingService>(sp =>
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Screening.API");
            var logger = sp.GetRequiredService<ILogger<OnboardingService>>();
            return new OnboardingService(client, logger);
        });

        return builder;
    }

    /// <summary>
    /// Configures a named HttpClient to talk to the Onboarding API with access token.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static WebApplicationBuilder ConfigureOnboardingApiClient(this WebApplicationBuilder builder, OnboardingApiOptions config)
    {
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddScoped(sp =>
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Screening.Idp");
            var logger = sp.GetRequiredService<ILogger<CustomAuthorizationMessageHandler>>();
            var cache = sp.GetRequiredService<IDistributedCache>();
            var options = sp.GetRequiredService<IOptions<OnboardingApiOptions>>();
            return new CustomAuthorizationMessageHandler(client, cache, options, logger);
        });

        builder.Services.AddHttpClient<OnboardingService>("Onboarding.API", client => client.BaseAddress = new Uri(config.Endpoint!))
            .AddPolicyHandler((sp, msg) => PolicyBuilder.GetRetryPolicy<OnboardingService>(sp, 5)) //retry 5 times
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (builder.Environment.IsDevelopment())
                {
                    //Disable certificate checking for IDP in DEV
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                }
                return handler;
            })
            .AddHttpMessageHandler<CustomAuthorizationMessageHandler>(); //use federated identity credentials
        return builder;
    }


}

internal static class PolicyBuilder
{
    public static Polly.IAsyncPolicy<HttpResponseMessage> GetRetryPolicy<TService>(IServiceProvider serviceProvider,
        int retryCount = 1)
        where TService : class
    {
        var logger = serviceProvider.GetRequiredService<ILogger<TService>>();
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(retryCount,
                retryAttempt => TimeSpan.FromSeconds(retryAttempt + Random.Shared.Next(0, 100) / 100D),
                onRetry: (result, span, index, ctx) =>
                {
                    logger.LogWarning("Retry attempt: {index} | Status: {statusCode}", index, result.Result?.StatusCode);
                });
    }
}
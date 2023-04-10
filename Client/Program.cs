using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Onboarding.Client;
using MudBlazor.Services;
using Onboarding.Client.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient<OnboardingService>("Onboarding.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
        .AddPolicyHandler((sp, msg) => Policy.WrapAsync(
        PolicyBuilder.GetFallbackPolicy<OnboardingService>(sp, OnboardingService.FallbackGetOnboardings),
        PolicyBuilder.GetRetryPolicy<OnboardingService>(sp)))
    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

// Supply HttpClient instances that include access tokens when making requests to the server project
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Onboarding.ServerAPI"));

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("api://4eeeb950-3fc9-4bd2-9279-51901fa33891/Onboarding.Read");
});

builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();



public static class PolicyBuilder
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

    public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy<TService>(IServiceProvider serviceProvider,
        Func<Context, CancellationToken, Task<HttpResponseMessage>> valueFactory)
        where TService : class
    {
        var logger = serviceProvider.GetRequiredService<ILogger<TService>>();
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .FallbackAsync(valueFactory, (res, ctx) =>
            {
                logger.LogWarning($"returning fallback value...");
                return Task.CompletedTask;
            });
    }
}
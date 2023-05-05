using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Authentication;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.Identity.Web;
using Onboarding.Server.Services;
using Polly;
using Polly.Extensions.Http;

namespace Onboarding.Server;

public static class Helpers
{
    /// <summary>
    /// Configures a DbContext based on the environment.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureDbContext(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine("Using in-memory database on env {0}", builder.Environment.EnvironmentName);
            builder.Services.AddDbContext<OnboardingDbContext>(
                options => options.UseInMemoryDatabase(databaseName: "OnboardingDb"));
        }
        else
        {
            Console.WriteLine("Using Azure SQL on env {0}", builder.Environment.EnvironmentName);
            builder.Services.AddDbContext<OnboardingDbContext>(
                options => options.UseSqlServer("name=ConnectionStrings:DefaultConnection"));
        }
        return builder;
    }

    /// <summary>
    /// Configures Azure KeyVault for production environments.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureSecrets(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsProduction())
        {
            return builder;
        }

        // Add services to the container.
        builder.Services
            .AddOptions<KeyVaultOptions>()
            .BindConfiguration(KeyVaultOptions.ConfigurationSectionName)
            .ValidateDataAnnotations();

        var config = builder.Configuration
            .GetRequiredSection(KeyVaultOptions.ConfigurationSectionName)
            .Get<KeyVaultOptions>()!;

        //configure keyvault with managed identity
        builder.Configuration.AddAzureKeyVault(
            new Uri(config.Endpoint!),
            new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = config.ClientId!
            }));
        return builder;
    }

    /// <summary>
    /// Configures Azure AD authentication for the API.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureAuth(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IAuthorizationHandler, RoleOrScopeHandler>();
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("ReadAccessPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new RoleOrScopeRequirement("Onboarding.ReadRole", "Onboarding.Read"));
            });

            options.AddPolicy("ReadWriteAccessPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new RoleOrScopeRequirement("Onboarding.ReadWriteRole", "Onboarding.ReadWrite"));
            });

            options.AddPolicy("ReadWriteScopeAccessPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireScope("Onboarding.ReadWrite");
            });
        });

        // Add services to the container.
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);

        //configure auth callbacks
        builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(30);
            
            var existingOnAuthFailedHandler = options.Events.OnAuthenticationFailed;
            options.Events.OnAuthenticationFailed = async context =>
            {
                await existingOnAuthFailedHandler(context);
            };

            var existingOnTokenValidatedHandler = options.Events.OnTokenValidated;
            options.Events.OnTokenValidated = async context =>
            {
                await existingOnTokenValidatedHandler(context);
            };

            var existingOnForbiddenHandler = options.Events.OnForbidden;
            options.Events.OnForbidden = async context =>
            {
                await existingOnForbiddenHandler(context);
            };
        });


        return builder;
    }

    /// <summary>
    /// Configures an HttpClient for the Screening API, to request screenings.
    /// Configures an HttpClient for the Screening IDP, to get tokens using the client credentials flow.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureScreeningService(this WebApplicationBuilder builder)
    {
        // Add services to the container.
        builder.Services
            .AddOptions<ScreeningApiOptions>()
            .BindConfiguration(ScreeningApiOptions.ConfigurationSectionName)
            .ValidateDataAnnotations();

        var config = builder.Configuration
            .GetRequiredSection(ScreeningApiOptions.ConfigurationSectionName)
            .Get<ScreeningApiOptions>()!;

        Console.WriteLine($"Screening API Config: {config}");

        builder
            .ConfigureScreeningIdpClient(config)
            .ConfigureScreeningApiClient(config);

        builder.Services.AddScoped<IExternalScreeningService>(sp =>
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Screening.API");
            var logger = sp.GetRequiredService<ILogger<ExternalScreeningService>>();
            return new ExternalScreeningService(client, logger);
        });

        return builder;
    }

    /// <summary>
    /// Configures a service to talk to Azure AD and manage user accounts.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static WebApplicationBuilder ConfigureAadService(this WebApplicationBuilder builder)
    {
        //steal kv config, to get managed identity client id
        var config = builder.Configuration
            .GetRequiredSection(KeyVaultOptions.ConfigurationSectionName)
            .Get<KeyVaultOptions>()!;

        string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
        var graphClient = new GraphServiceClient(new AzureIdentityAuthenticationProvider(new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = config.ClientId!
        }), scopes: scopes));
        builder.Services.AddScoped(sp => graphClient);
        builder.Services.AddScoped<IAzureAdManagementService, AzureAdManagementService>();
        return builder;
    }

    /// <summary>
    /// Configures a named HttpClient to talk to the Screening API with access token.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static WebApplicationBuilder ConfigureScreeningApiClient(this WebApplicationBuilder builder, ScreeningApiOptions config)
    {
        builder.Services.AddScoped(sp =>
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Screening.Idp");
            var logger = sp.GetRequiredService<ILogger<CustomAuthorizationMessageHandler>>();
            var cache = sp.GetRequiredService<IDistributedCache>();
            var options = sp.GetRequiredService<IOptions<ScreeningApiOptions>>();
            return new CustomAuthorizationMessageHandler(client, cache, options, logger);
        });

        builder.Services.AddHttpClient<ExternalScreeningService>("Screening.API", client => client.BaseAddress = new Uri(config.Endpoint!))
            .AddPolicyHandler((sp, msg) => PolicyBuilder.GetRetryPolicy<ExternalScreeningService>(sp, 5)) //retry 5 times
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
            .AddHttpMessageHandler<CustomAuthorizationMessageHandler>(); //use client credentials
        return builder;
    }

    /// <summary>
    /// Configures a named HttpClient to talk to the Screening API's Identity Provider, used to get access tokens for the API.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    private static WebApplicationBuilder ConfigureScreeningIdpClient(this WebApplicationBuilder builder, ScreeningApiOptions config)
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

        return builder;
    }
}

/// <summary>
/// Requirement for either app role or scope
/// </summary>
public class RoleOrScopeRequirement : IAuthorizationRequirement
{
    public string Role { get; }
    public string Scope { get; }

    public RoleOrScopeRequirement(string role, string scope)
    {
        Role = role;
        Scope = scope;
    }
}

/// <summary>
/// Authorization handler for either app role or scope
/// </summary>
public class RoleOrScopeHandler : AuthorizationHandler<RoleOrScopeRequirement>
{
    private const string ScopeClaimType = "http://schemas.microsoft.com/identity/claims/scope";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, RoleOrScopeRequirement requirement)
    {
        if (context.User.HasClaim(c => c.Type == ScopeClaimType && c.Value.Contains(requirement.Scope)) //user with proper scope
            || context.User.IsInRole(requirement.Role)) //app with proper role
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

internal static class PolicyBuilder
{
    /// <summary>
    /// Configures a retry policy for HTTP requests
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <param name="serviceProvider"></param>
    /// <param name="retryCount"></param>
    /// <returns></returns>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy<TService>(IServiceProvider serviceProvider,
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Onboarding.Server.Services;

namespace Onboarding.Server;

public static class Helpers
{
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
        });


        return builder;
    }

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

        builder.Services.AddScoped<IExternalScreeningService>(sp =>
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Screening.API");
            var logger = sp.GetRequiredService<ILogger<ExternalScreeningService>>();
            return new ExternalScreeningService(client, logger);
        });

        return builder;
    }
}

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
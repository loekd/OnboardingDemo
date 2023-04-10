using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace ExternalScreening.Api
{
    /// <summary>
    /// Can be bound from configuration to set up Identity Server
    /// </summary>
    public class IdentityServerOptions
    {
        public const string ConfigurationSectionName = "IdentityServer";

        /// <summary>
        /// Location of Identity Server
        /// </summary>
        [StringLength(128, MinimumLength = 20)]
        public string Authority { get; set; } = "https://localhost:7242";

        /// <summary>
        /// Identifier of this API resource.
        /// The required value of the audience (aud) claim in the token.
        /// Prevents token forwarding to other services.
        /// </summary>
        [StringLength(128, MinimumLength = 2)]
        public string Audience { get; set; } = "ScreeningAPI";

        /// <summary>
        /// The scope required to access this API
        /// </summary>
        [StringLength(128, MinimumLength = 2)]
        public string RequiredReadWriteScope { get; set; } = "Screening.ReadWrite";

        /// <summary>
        /// Allowed token issuer FQDN, concatenated by ';'
        /// </summary>
        public string Issuers { get; set; } = "https://localhost:7242";

        public override string ToString()
        {
            return $"Auth: {Authority} - Aud: {Audience} - Scp: {RequiredReadWriteScope} - Iss:{Issuers}";
        }
    }

    public static class IdentityServerExtensions
    {
        private const string ApiPolicyName = "ApiPolicy";

        public static void ConfigureIdentityServer(this WebApplicationBuilder builder)
        {
            var idsrvConfig = builder.Configuration
                .GetRequiredSection(IdentityServerOptions.ConfigurationSectionName)
                .Get<IdentityServerOptions>()!;

            Console.WriteLine($"Identity Server Config: {idsrvConfig}");
            Console.WriteLine($"Environment IdentityServer__Authority: {Environment.GetEnvironmentVariable("IdentityServer__Authority")}");

            //Use JWT bearer tokens for authentication
            builder.Services.AddAuthentication("Bearer")
                        .AddJwtBearer(options =>
                        {
                            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
                            options.Authority = idsrvConfig.Authority;
                            options.TokenValidationParameters.ValidateAudience = true;
                            options.TokenValidationParameters.ValidAudience = idsrvConfig.Audience;
                            if (!string.IsNullOrWhiteSpace(idsrvConfig.Issuers))
                            {
                                options.TokenValidationParameters.ValidIssuers = idsrvConfig.Issuers.Split(';'); //new[] { "https://localhost:8445", "http://screening-idp", "https://localhost:7242" }; //container through host, container direct, standalone
                            }
                            else
                            {
                                options.TokenValidationParameters.ValidIssuer = idsrvConfig.Authority;
                            }
                            options.RequireHttpsMetadata = false;

                            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents();
                            options.Events.OnAuthenticationFailed = ctx =>
                            {
                                return Task.CompletedTask;
                            };

                            options.Events.OnTokenValidated = ctx =>
                            {
                                return Task.CompletedTask;
                            };
                        });


            //Require authenticated users
            //Require read-write scope
            builder.Services.AddAuthorization(options =>
                options.AddPolicy(ApiPolicyName, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", idsrvConfig.RequiredReadWriteScope);
                })
            );
        }
    }
}
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.DataProtection;
using Secret = Duende.IdentityServer.Models.Secret;

namespace ExternalScreening.Api.IdSrv;

public static class Config
{
    public const string ScreeningReadWriteScope = "Screening.ReadWrite";

    public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResource()
            {
                Name = "verification",
                UserClaims = new List<string>
                {
                    JwtClaimTypes.Email,
                    JwtClaimTypes.EmailVerified
                }
            }
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new ApiScope(ScreeningReadWriteScope, "Screening Api Scope")
            {
                Description = "Access Screening API."
            }
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new List<ApiResource>
        {
            new ApiResource("ScreeningAPI", "Screening Api Resource")
            {
                Description = "Screening API resource",
                Scopes = { ScreeningReadWriteScope },
                UserClaims = { "name", "email", "role" },
            }
        };

    public static IEnumerable<Client> Clients =>
        new List<Client>
        {
            // machine-to-machine client
            new Client
            {
                ClientId = "client",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                // scopes that client has access to
                AllowedScopes = { ScreeningReadWriteScope }
            }
        };
}

using System.ComponentModel.DataAnnotations;

namespace ExternalScreening.Api.Services;

/// <summary>
/// Configuration that specifies parameters needed to call the Onboarding API using Federated credentials.
/// </summary>
public class OnboardingApiOptions
{
    public const string ConfigurationSectionName = "OnboardingApi";

    /// <summary>
    /// Location of Identity Server (in the Cloud)
    /// </summary>
    [StringLength(128, MinimumLength = 20)]
    public string Authority { get; set; } = "https://ca-identity-server.politewater-ba7a3a0c.westeurope.azurecontainerapps.io";

    /// <summary>
    /// Base address of onboarding API
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string? Endpoint { get; set; }

    /// <summary>
    /// ClientId of special client in Identity Server, tailored to emit tokens that AAD trusts and understands.
    /// No Azure AD credentials are needed.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string? IdentityServerClientId { get; set; } = "onboardingapi";

    /// <summary>
    /// Client Secret to go with <see cref="IdentityServerClientId"/>.
    /// No Azure AD credentials needed.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string? IdentityServerClientSecret { get; set; }

    /// <summary>
    /// The scope requested to access the onboarding API
    /// </summary>
    [StringLength(128, MinimumLength = 2)]
    public string IdentityServerRequestedReadWriteScope { get; set; } = "Onboarding.ReadWrite";


    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string? AzureAdClientId { get; set; } = "5fc77057-af27-4195-834a-71ac6fb55e1b";

    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string? AzureAdRequestedScope { get; set; } = "api://4eeeb950-3fc9-4bd2-9279-51901fa33891/.default";

    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string? AzureAdAuthority { get; set; } = "https://login.microsoftonline.com/9c872e88-865c-4c4e-b258-0934b470991d";

    public override string ToString()
    {
        return $"IdSrvEndpoint: {Endpoint} - IdSrvClientId: {IdentityServerClientId} - IdSrvClientSecret:{IdentityServerClientSecret} - IdSrvScp: {IdentityServerRequestedReadWriteScope} - AadAuthority: {AzureAdAuthority} - AadClientId: {AzureAdClientId} - AadScp: {AzureAdRequestedScope}";
    }
}

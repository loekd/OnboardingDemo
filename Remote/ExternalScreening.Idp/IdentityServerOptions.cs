using System.ComponentModel.DataAnnotations;

namespace ExternalScreening.Idp
{
    /// <summary>
    /// Can be bound from configuration to set up Identity Server as an IDP.
    /// </summary>
    public class IdentityServerOptions
    {
        public const string ConfigurationSectionName = "IdentityServer";

        /// <summary>
        /// Object ID of the managed identity that is used to impersonate an Onboarding API client.
        /// </summary>
        [StringLength(128, MinimumLength = 20)]
        public string? ImpersonationIdentityObjectId { get; set; }

        public override string ToString()
        {
            return $"ImpersonationIdentityObjectId: {ImpersonationIdentityObjectId}";
        }
    }
}
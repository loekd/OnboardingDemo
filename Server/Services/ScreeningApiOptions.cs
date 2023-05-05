using System.ComponentModel.DataAnnotations;

namespace Onboarding.Server.Services
{
    public class ScreeningApiOptions
    {
        public const string ConfigurationSectionName = "ScreeningApi";

        [Required]
        [StringLength(200, MinimumLength = 10)]
        public string? Endpoint { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string? ClientId { get; set; } = "screeningapi";

        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string? ClientSecret { get; set; }

        /// <summary>
        /// Location of Identity Server
        /// </summary>
        [StringLength(128, MinimumLength = 20)]
        public string Authority { get; set; } = "https://localhost:7242";

        /// <summary>
        /// The scope requested to access the screening API
        /// </summary>
        [StringLength(128, MinimumLength = 2)]
        public string RequestedReadWriteScope { get; set; } = "Screening.ReadWrite";

        public override string ToString()
        {
            return $"Endpoint: {Endpoint} - ClientId: {ClientId} - ClientSecret:{ClientSecret} - Auth: {Authority} - Scp: {RequestedReadWriteScope}";
        }
    }
}

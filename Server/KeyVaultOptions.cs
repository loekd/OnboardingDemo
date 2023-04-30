using System.ComponentModel.DataAnnotations;

namespace Onboarding.Server;

public class KeyVaultOptions
{
    public const string ConfigurationSectionName = "KeyVault";

    /// <summary>
    /// Vault URI
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 10)]
    public string? Endpoint { get; set; }

    /// <summary>
    /// User-assigned managed identity ClientId.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string? ClientId { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Onboarding.Shared;

public enum Status
{
    Unknown = 0,
    Pending = 1,
    Skipped = 2,
    Passed = 3,
    NotPassed = 4
}

public record OnboardingModel(Guid Id, string FirstName, string LastName, Status Status, string? ImageUrl = null, Guid? ExternalScreeningId = null);

public class CreateOnboardingModel(Guid id, string firstName, string lastName )
{
    [Required]
    public string FirstName { get; set; } = firstName;

    [Required]
    public string LastName { get; set; } = lastName;

    public Guid Id { get; set; } = id;

    public Status Status { get; set; } = Status.Unknown;

    public bool RequestExternalScreening { get; set; } = false;

    public CreateOnboardingModel() : this(Guid.NewGuid(), string.Empty, string.Empty)
    {
        Status = Status.Unknown;
    }
}

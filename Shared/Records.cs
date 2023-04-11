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

public class CreateOnboardingModel
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    public Guid Id { get; set; }

    public Status Status { get; set; } = Status.Unknown;

    public bool RequestExternalScreening { get; set; } = false;

    public CreateOnboardingModel() : this(Guid.NewGuid(), string.Empty, string.Empty)
    {
    }

    public CreateOnboardingModel(Guid id, string firstName, string lastName, Status? status = Status.Unknown, bool requestExternalScreening = false)
    {
        Id = id;
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Status = status ?? Status.Unknown;
        RequestExternalScreening = requestExternalScreening;
    }
}

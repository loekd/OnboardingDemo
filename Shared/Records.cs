using System.Xml.Linq;

namespace Onboarding.Shared;

public enum Status
{
    Unknown = 0,
    Pending = 1,
    Skipped = 2,
    Approved = 3,
    NotApproved = 4
}

public record OnboardingModel(Guid Id, string FirstName, string LastName, Status Status);

public class CreateOnboardingModel(Guid id, string firstName, string lastName )
{
    public string FirstName { get; set; } = firstName;

    public string LastName { get; set; } = lastName;

    public Guid Id { get; set; } = id;

    public Status Status { get; set; } = Status.Unknown;

    public bool RequestExternalScreening { get; set; } = false;

    public CreateOnboardingModel() : this(Guid.NewGuid(), string.Empty, string.Empty)
    {
        Status = Status.Unknown;
    }
}

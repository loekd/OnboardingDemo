using System.ComponentModel.DataAnnotations;

namespace ExternalScreening.Api.Models;

public record CreateScreeningRequest([Required] string FirstName, [Required] string LastName, [Required] Guid OnboardingId);

public record CreateScreeningResponse(Guid ScreeningId, string FirstName, string LastName, Guid OnboardingId);

public record ScreeningResult(Guid ScreeningId, string FirstName, string LastName, bool? IsApproved, Guid OnboardingId);
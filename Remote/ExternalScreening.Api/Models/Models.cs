using System.ComponentModel.DataAnnotations;

namespace ExternalScreening.Api.Models;

public record CreateScreeningRequest([Required] string FirstName, [Required] string LastName);

public record CreateScreeningResponse(Guid ScreeningId, string FirstName, string LastName);

public record ScreeningResult(string FirstName, string LastName, bool Approved);
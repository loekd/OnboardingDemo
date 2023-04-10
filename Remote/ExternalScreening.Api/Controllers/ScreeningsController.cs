using ExternalScreening.Api.Models;
using ExternalScreening.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExternalScreening.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ApiPolicy")]
public class ScreeningsController : ControllerBase
{
    private readonly IScreeningService _screeningService;
    private readonly ILogger<ScreeningsController> _logger;

    public ScreeningsController(IScreeningService screeningService, ILogger<ScreeningsController> logger)
    {
        _screeningService = screeningService ?? throw new ArgumentNullException(nameof(screeningService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> AddScreeningAsync([FromBody]CreateScreeningRequest request)
    {
        _logger.LogTrace("Adding screening");

        var screening = new ScreeningEntity(request.FirstName, request.LastName);
        await _screeningService.AddScreening(screening);
        var response = new CreateScreeningResponse(screening.Id, request.FirstName, request.LastName);

        return Ok(response);
    }

    [HttpGet("{screeningId:guid}")]
    public async Task<IActionResult> GetScreeningAsync(Guid screeningId)
    {
        _logger.LogTrace("Fetch screening {Id}", screeningId);

        var screening = await _screeningService.GetScreening(screeningId);
        if (screening == null) { return NotFound(); }

        var response = new ScreeningResult(screening.FirstName, screening.LastName, screening.IsApproved);

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetScreenings()
    {
        _logger.LogTrace("Fetch all screenings");

        var screenings = await _screeningService.GetScreenings();
        var response = screenings.Select(s => new ScreeningResult(s.FirstName, s.LastName, s.IsApproved));

        return Ok(response);
    }

}
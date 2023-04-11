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

    [HttpPost("{screeningId:guid}/approve")]
    public async Task<IActionResult> ApproveScreeningAsync(Guid screeningId)
    {
        var screening = await _screeningService.GetScreening(screeningId);
        if (screening == null) { return NotFound(); }

        _logger.LogTrace("Approve screening {Id}", screeningId);
      
        await _screeningService.UpdateScreeningStatus(screeningId, true);

        var response = new ScreeningResult(screeningId, screening.FirstName, screening.LastName, true);
        return Ok(response);
    }

    [HttpPost("{screeningId:guid}/disapprove")]
    public async Task<IActionResult> DisapproveScreeningAsync(Guid screeningId)
    {
        var screening = await _screeningService.GetScreening(screeningId);
        if (screening == null) { return NotFound(); }

        _logger.LogTrace("Disapprove screening {Id}", screeningId);

        await _screeningService.UpdateScreeningStatus(screeningId, false);

        var response = new ScreeningResult(screeningId, screening.FirstName, screening.LastName, false);
        return Ok(response);
    }

    [HttpPost("{screeningId:guid}/delay")]
    public async Task<IActionResult> DelayScreeningAsync(Guid screeningId)
    {
        var screening = await _screeningService.GetScreening(screeningId);
        if (screening == null) { return NotFound(); }

        _logger.LogTrace("Delay screening {Id}", screeningId);

        await _screeningService.UpdateScreeningStatus(screeningId, null);

        var response = new ScreeningResult(screeningId, screening.FirstName, screening.LastName, null);
        return Ok(response);
    }

    [HttpGet("{screeningId:guid}")]
    public async Task<IActionResult> GetScreeningAsync(Guid screeningId)
    {
        _logger.LogTrace("Fetch screening {Id}", screeningId);

        var screening = await _screeningService.GetScreening(screeningId);
        if (screening == null) { return NotFound(); }

        var response = new ScreeningResult(screeningId, screening.FirstName, screening.LastName, screening.IsApproved);

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetScreenings()
    {
        _logger.LogTrace("Fetch all screenings");

        var screenings = await _screeningService.GetScreenings();
        var response = screenings.Select(s => new ScreeningResult(s.Id, s.FirstName, s.LastName, s.IsApproved));

        return Ok(response);
    }

}
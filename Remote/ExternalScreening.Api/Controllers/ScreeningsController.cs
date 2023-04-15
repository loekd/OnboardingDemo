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
    private readonly IOnboardingService _customerNotificationService;
    private readonly ILogger<ScreeningsController> _logger;

    public ScreeningsController(IScreeningService screeningService, IOnboardingService customerNotificationService, ILogger<ScreeningsController> logger)
    {
        _screeningService = screeningService ?? throw new ArgumentNullException(nameof(screeningService));
        _customerNotificationService = customerNotificationService ?? throw new ArgumentNullException(nameof(customerNotificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost]
    public async Task<IActionResult> AddScreeningAsync([FromBody]CreateScreeningRequest request)
    {
        _logger.LogTrace("Adding screening");

        var screening = new ScreeningEntity(request.FirstName, request.LastName, request.OnboardingId);
        await _screeningService.AddScreening(screening);
        var response = new CreateScreeningResponse(screening.Id, request.FirstName, request.LastName, request.OnboardingId);

        return Ok(response);
    }

    [HttpPost("{screeningId:guid}/approve")]
    public async Task<IActionResult> ApproveScreeningAsync(Guid screeningId)
    {
        var screening = await _screeningService.GetScreening(screeningId);
        if (screening == null) { return NotFound(); }

        _logger.LogTrace("Approve screening {Id}", screeningId);

        screening.IsApproved = true;
        await _screeningService.UpdateScreeningStatus(screening);
        
        //notify onboarding
        await _customerNotificationService.SendScreeningResultAsync(screening);

        var response = new ScreeningResult(screeningId, screening.FirstName, screening.LastName, true, screening.OnboardingId);
        return Ok(response);
    }

    [HttpPost("{screeningId:guid}/disapprove")]
    public async Task<IActionResult> DisapproveScreeningAsync(Guid screeningId)
    {
        var screening = await _screeningService.GetScreening(screeningId);
        if (screening == null) { return NotFound(); }

        _logger.LogTrace("Disapprove screening {Id}", screeningId);

        screening.IsApproved = false;
        await _screeningService.UpdateScreeningStatus(screening);

        //notify onboarding
        await _customerNotificationService.SendScreeningResultAsync(screening);

        var response = new ScreeningResult(screeningId, screening.FirstName, screening.LastName, false, screening.OnboardingId);
        return Ok(response);
    }

    [HttpGet("{screeningId:guid}")]
    public async Task<IActionResult> GetScreeningAsync(Guid screeningId)
    {
        _logger.LogTrace("Fetch screening {Id}", screeningId);

        var screening = await _screeningService.GetScreening(screeningId);
        if (screening == null) { return NotFound(); }

        var response = new ScreeningResult(screeningId, screening.FirstName, screening.LastName, screening.IsApproved, screening.OnboardingId);

        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetScreenings()
    {
        _logger.LogTrace("Fetch all screenings");

        var screenings = await _screeningService.GetScreenings();
        var response = screenings.Select(s => new ScreeningResult(s.Id, s.FirstName, s.LastName, s.IsApproved, s.OnboardingId));

        return Ok(response);
    }

}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Onboarding.Server.Services;
using Onboarding.Shared;

namespace Onboarding.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingsController : ControllerBase
    {
        private readonly IOnboardingDataService _onboardingService;
        private readonly IExternalScreeningService _screeningService;
        private readonly IAzureAdManagementService _azureAdManagementService;
        private readonly ILogger<OnboardingsController> _logger;

        public OnboardingsController(IOnboardingDataService onboardingService, IExternalScreeningService screeningService, IAzureAdManagementService azureAdManagementService, ILogger<OnboardingsController> logger)
        {
            _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
            _screeningService = screeningService ?? throw new ArgumentNullException(nameof(screeningService));
            _azureAdManagementService = azureAdManagementService ?? throw new ArgumentNullException(nameof(azureAdManagementService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Fetches all onboardings. Can be called by machines and humans.
        /// </summary>
        /// <returns></returns>
        [Authorize(policy: "ReadAccessPolicy")]
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            _logger.LogTrace("Fetching all onboardings.");

            var all = await _onboardingService.GetOnboardings();

            return Ok(all.Select(o => new OnboardingModel(o.Id, o.FirstName!, o.LastName!, o.Status, o.ImageUrl, o.ExternalScreeningId, o.AzureAdAccountId)));
        }

        /// <summary>
        /// Creates a new onboarding. Can be called by humans.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [Authorize(policy: "ReadWriteScopeAccessPolicy")]
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] CreateOnboardingModel input)
        {
            input.Id = Guid.NewGuid();

            //request screening if needed
            if (input.RequestExternalScreening)
            {
                _logger.LogTrace("Requesting screening for Onboarding {OnboardingId} with screening.", input.Id);
                var response = await _screeningService.RequestScreening(new CreateScreeningRequest(input.FirstName, input.LastName, input.Id));

                //store locally
                _logger.LogTrace("Registering new Onboarding {OnboardingId}.", input.Id);
                await _onboardingService.AddOnboarding(new OnboardingEntity(input.Id, input.FirstName, input.LastName, Status.Pending, "media/Man03.png", externalScreeningId: response.ScreeningId));
            }
            else
            {
                _logger.LogTrace("Registering new Onboarding {OnboardingId} without screening.", input.Id);

                //store locally
                await _onboardingService.AddOnboarding(new OnboardingEntity(input.Id, input.FirstName, input.LastName, Status.Skipped, "media/Woman03.png"));
            }

            return Ok(input);
        }

        /// <summary>
        /// Deletes an existing onboarding. Can be called by machines and humans.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [Authorize(policy: "ReadWriteScopeAccessPolicy")]
        [HttpDelete("{onboardingId:guid}")]
        public async Task<IActionResult> DeleteAsync([FromRoute] Guid onboardingId)
        {
            _logger.LogTrace("Deleting onboarding {OnboardingId}.", onboardingId);

            var existing = await _onboardingService.GetOnboarding(onboardingId);
            if (existing is null)
            {
                return NotFound();
            }

            await _onboardingService.DeleteOnboarding(existing);
            return NoContent();
        }

        /// <summary>
        /// Updates an existing onboarding with screening result.
        /// Can only be called by the external screening API to return the screening result, not by human users.
        /// </summary>
        /// <param name="onboardingId"></param>
        /// <param name="screeningId"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        [Authorize(Roles = "Onboarding.ReadWriteRole")]
        [HttpPost("{onboardingId:guid}/screenings/{screeningId:guid}")]
        public async Task<IActionResult> PostScreeningResultAsync(Guid onboardingId, Guid screeningId, [FromBody] ScreeningResult input)
        {
            _logger.LogTrace("Registering screening result for onboarding {OnboardingId} with screening {ScreeningID}.", onboardingId, screeningId);

            //find onboarding
            var onboarding = await _onboardingService.GetOnboarding(onboardingId);
            if (onboarding == null
                || onboarding.ExternalScreeningId != screeningId
                || onboarding.FirstName != input.FirstName
                || onboarding.LastName != input.LastName)
            {
                return NotFound();
            }

            if (input.IsApproved.HasValue)
            {
                if (input.IsApproved.Value)
                {
                    onboarding.Status = Status.Passed;

                    //create user account in AAD when screening result is positive
                    _logger.LogTrace("Creating user account for {FirstName}. Screening result positive.", input.FirstName);
                    var aadUser = await _azureAdManagementService.CreateUser(input.FirstName, input.LastName);
                    onboarding.AzureAdAccountId = aadUser.Id;
                }
                else
                {
                    onboarding.Status = Status.NotPassed;

                    //delete user if it exists
                    _logger.LogTrace("Deleting user account for {FirstName}. Screening result negative.", input.FirstName);
                    await _azureAdManagementService.DeleteUser(input.FirstName, input.LastName);
                    onboarding.AzureAdAccountId = null;
                }
            }
            else
            {
                onboarding.Status = Status.Pending;
            }

            //store locally
            await _onboardingService.UpdateOnboarding(onboarding);

            return Ok(input);
        }
    }
}

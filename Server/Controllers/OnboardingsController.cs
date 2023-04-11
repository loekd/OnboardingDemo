using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using Onboarding.Server.Services;
using Onboarding.Shared;

namespace Onboarding.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class OnboardingsController : ControllerBase
    {
        private readonly IOnboardingDataService _onboardingService;
        private readonly IExternalScreeningService _screeningService;
        private readonly ILogger<OnboardingsController> _logger;

        public OnboardingsController(IOnboardingDataService onboardingService, IExternalScreeningService screeningService, ILogger<OnboardingsController> logger)
        {
            _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
            _screeningService = screeningService ?? throw new ArgumentNullException(nameof(screeningService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            _logger.LogTrace("Fetching all onboardings.");

            var all = await _onboardingService.GetOnboardings();
                
            return Ok(all.Select(o => new OnboardingModel(o.Id, o.FirstName!, o.LastName!, o.Status, o.ImageUrl, o.ExternalScreeningId)));
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody]CreateOnboardingModel input)
        {
            if (input.RequestExternalScreening)
            {
                _logger.LogTrace("Registering new onboarding with screening.");

                //request screening with external screening service
                var response = await _screeningService.RequestScreening(new CreateScreeningRequest(input.FirstName, input.LastName));

                //store locally
                await _onboardingService.AddOnboarding(new OnboardingEntity(input.Id, input.FirstName, input.LastName, Status.Pending, "media/Man03.png", externalScreeningId: response.ScreeningId));
            }
            else
            {
                _logger.LogTrace("Registering new onboarding.");

                //store locally
                await _onboardingService.AddOnboarding(new OnboardingEntity(input.Id, input.FirstName, input.LastName, Status.Skipped, "media/Woman03.png"));
            }
            
            return Ok(input);
        }
    }
}

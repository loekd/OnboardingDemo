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
                
            return Ok(all.Select(o => new OnboardingModel(o.Id, o.FirstName!, o.LastName!, o.Status)));
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody]CreateOnboardingModel input)
        {
            _logger.LogTrace("Registering new onboarding.");

            if (input.RequestExternalScreening)
            {
                //store locally
                await _onboardingService.AddOnboarding(new OnboardingEntity(input.Id, input.FirstName, input.LastName, Status.Pending));
                //request screening with external screening service
                await _screeningService.RequestScreening(new CreateScreeningRequest(input.FirstName, input.LastName));
            }
            else
                //store locally
                await _onboardingService.AddOnboarding(new OnboardingEntity(input.Id, input.FirstName, input.LastName, Status.Skipped));

            
            return Ok(input);
        }
    }
}

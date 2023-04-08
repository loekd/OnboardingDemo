using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using Onboarding.Shared;

namespace Onboarding.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes")]
    public class OnboardingsController : ControllerBase
    {
        private readonly ILogger<OnboardingsController> logger;
        private static readonly List<OnboardingModel> Onboardings = new List<OnboardingModel>
            {
                new OnboardingModel(Guid.NewGuid(), "John", "Doe", Status.Pending),
                new OnboardingModel(Guid.NewGuid(), "Jane", "Doe", Status.Approved),
                new OnboardingModel(Guid.NewGuid(), "Jack", "Doe", Status.NotApproved),
                new OnboardingModel(Guid.NewGuid(), "Joey", "Doe", Status.Skipped)
            };

        public OnboardingsController(ILogger<OnboardingsController> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(Onboardings);
        }

        [HttpPost]
        public IActionResult Post([FromBody]CreateOnboardingModel input)
        {
            if (input.RequestExternalScreening)
                Onboardings.Add(new OnboardingModel(input.Id, input.FirstName, input.LastName, Status.Pending));
            else
                Onboardings.Add(new OnboardingModel(input.Id, input.FirstName, input.LastName, Status.Skipped));

            return Ok(input);
        }
    }
}

using Onboarding.Shared;

namespace Onboarding.Client.Shared
{
    public class OnboardingDetailsModel : DialogComponent<Onboarding.Shared.OnboardingModel>
    {
        /// <summary>
        /// Create new value if needed.
        /// </summary>
        /// <returns></returns>
        protected override Task OnInitializedAsync()
        {
            if (SelectedItem is null)
            {
                SelectedItem = new Onboarding.Shared.OnboardingModel(Guid.NewGuid(), string.Empty, string.Empty, Status.Unknown);
                IsNew = true;
            }

            return base.OnInitializedAsync();
        }
    }
}

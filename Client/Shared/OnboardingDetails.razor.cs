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
            if (base.SelectedItem is null)
            {
                base.SelectedItem = new Onboarding.Shared.OnboardingModel(Guid.NewGuid(), string.Empty, string.Empty, Status.Unknown);
                base.IsNew = true;
            }

            return base.OnInitializedAsync();
        }
    }
}

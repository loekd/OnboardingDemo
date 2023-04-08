using Onboarding.Shared;

namespace Onboarding.Client.Shared
{
    public class CreateOnboardingModel : DialogComponent<Onboarding.Shared.CreateOnboardingModel>
    {
        /// <summary>
        /// Create new value if needed.
        /// </summary>
        /// <returns></returns>
        protected override Task OnInitializedAsync()
        {
            if (base.SelectedItem is null)
            {
                base.SelectedItem = new Onboarding.Shared.CreateOnboardingModel();
                base.IsNew = true;
            }

            return base.OnInitializedAsync();
        }
    }
}

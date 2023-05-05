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
            if (SelectedItem is null)
            {
                SelectedItem = new Onboarding.Shared.CreateOnboardingModel();
                IsNew = true;
            }

            return base.OnInitializedAsync();
        }

        protected override Task DeleteImpl()
        {
            throw new NotImplementedException();
        }
    }
}

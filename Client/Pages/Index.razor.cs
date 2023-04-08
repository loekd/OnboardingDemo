using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using MudBlazor;
using Onboarding.Client.Shared;
using Onboarding.Shared;

namespace Onboarding.Client.Pages;

[Authorize]
public class IndexModel : Component<Onboarding.Shared.OnboardingModel>
{
    public bool MiniCheckInPopOverIsOpen { get; set; } = false;

    /// <summary>
    /// Fetch status
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    /// <summary>
    /// Refreshes all screen data.
    /// </summary>
    /// <returns></returns>
    private async Task LoadData()
    {
        base.IsLoading = true;
        try
        {
            base.Items.Clear();
            var result = await base.OnboardingService!.GetAll();
            base.Items.AddRange(result);
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
        }
        catch (Exception ex)
        {
            base.Logger!.LogError("Failed to fetch onboarding data. Error: {ErrorMessage}", ex.Message);
        }
        finally
        {
            base.IsLoading = false;
        }
    }


    /// <summary>
    /// Shows the create dialog and handles the result.
    /// </summary>
    /// <param name="beer"></param>
    /// <returns></returns>
    protected async Task HandleCreateDialog()
    {
        MiniCheckInPopOverIsOpen = true;

        var parameters = new DialogParameters
        {
            [$"{nameof(SelectedItem)}"] = new Onboarding.Shared.CreateOnboardingModel()
        };

        var dialog = DialogService!.Show<CreateOnboarding>("", parameters);
        
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            var newOnboarding = (Onboarding.Shared.CreateOnboardingModel)result.Data;
            try
            {
                await OnboardingService!.Add(newOnboarding);
                await LoadData();
            }
            catch (AccessTokenNotAvailableException ex)
            {
                ex.Redirect();
            }
            catch (Exception ex)
            {
                base.Logger!.LogError("Failed to add onboarding data. Error: {ErrorMessage}", ex.Message);
            }
        }

        MiniCheckInPopOverIsOpen = false;
    }
}

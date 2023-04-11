using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Onboarding.Client.Services;

namespace Onboarding.Client.Shared
{
    public abstract class Component<T> : ComponentBase
    {
        [Inject]
        public IDialogService? DialogService { get; set; }

        [Inject]
        public ILogger<Component<T>>? Logger { get; set; }

        [Inject]
        public ISnackbar? Toaster { get; set; }

        [Inject]
        public IOnboardingService? OnboardingService { get; set; }

        [Inject]
        public IJSRuntime? JSRuntime { get; set; }

        /// <summary>
        /// Is 'true' when the model is being saved.
        /// </summary>
        public bool IsSaving { get; set; } = false;

        /// <summary>
        /// Is 'true' when the model is being loaded.
        /// </summary>
        public bool IsLoading { get; set; } = true;

        /// <summary>
        /// Gets or sets the selected item. If null, we're adding a new item.
        /// </summary>
        [Parameter]
        public T? SelectedItem { get; set; }

        /// <summary>
        /// Gets or sets the selected item. If null, we're adding a new item.
        /// </summary>
        public List<T> Items { get; } = new List<T>();

        protected void RowClicked(TableRowClickEventArgs<T> selectedRowItem)
        {
            if (selectedRowItem.Item != null)
            {
                SelectedItem = selectedRowItem.Item;
            }
        }
    }

    /// <summary>
    /// Component that is a dialog that edits or displays an entity <typeparamref name="T"/>.
    /// </summary>
    public abstract class DialogComponent<T> : Component<T>
    {
        [CascadingParameter]
        public MudDialogInstance? MudDialog { get; set; }

        /// <summary>
        /// Indicates whether we're editing or viewing.
        /// </summary>
        [Parameter]
        public bool IsEditing { get; set; } = false;

        /// <summary>
        /// Indicates whether the current instance is new.
        /// </summary>
        [Parameter]
        public bool IsNew { get; set; } = false;

        /// <summary>
        /// Close dialog without saving
        /// </summary>
        protected virtual void Cancel()
        {
            MudDialog!.Cancel();
        }

        /// <summary>
        /// Close dialog with result.
        /// </summary>
        protected virtual Task Save()
        {
            MudDialog!.Close(DialogResult.Ok(SelectedItem));
            return Task.CompletedTask;
        }
    }
}

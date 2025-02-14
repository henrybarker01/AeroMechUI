using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Components
{
    public partial class ConfirmationModal
    {
        [Inject] protected ConfirmationService _confirmationService { get; set; }

        private bool IsVisible = false;
        private string Message = "";
        private TaskCompletionSource<bool>? TaskSource;

        protected override void OnInitialized()
        {

        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                _confirmationService.OnShow += ShowModal;

            return base.OnAfterRenderAsync(firstRender);
        }

        private Task<bool> ShowModal(string message)
        {
            IsVisible = true;
            Message = message;
            TaskSource = new TaskCompletionSource<bool>();
            InvokeAsync(StateHasChanged);
            return TaskSource.Task;
        }

        private void Confirm(bool result)
        {
            IsVisible = false;
            TaskSource?.SetResult(result);
            // StateHasChanged();
        }

        public void Dispose()
        {
            _confirmationService.OnShow -= ShowModal;
        }
    }
}
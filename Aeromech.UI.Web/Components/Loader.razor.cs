using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Components
{
    public partial class Loader
    {
        [Inject] protected LoaderService _loaderService { get; set;  }
        protected bool isLoading { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
                _loaderService.OnLoadingChanged += SetLoading;
            
        }
         
        private async void SetLoading(bool isLoading)
        {
            this.isLoading = isLoading;
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            _loaderService.OnLoadingChanged -= SetLoading;
        }
    }
}
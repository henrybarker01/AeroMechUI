using Microsoft.AspNetCore.Components;
using AeroMech.UI.Web.Components;
using BlazorBootstrap;

namespace AeroMech.UI.Web.Shared
{
    public partial class MainLayout: LayoutComponentBase
    {
        private bool IsLoading = true;

        private async Task FetchData()
        {
            IsLoading = true;
            await Task.Delay(3000); // Simulate DB fetch
            IsLoading = false;
        }
    }
}

using AeroMech.Models.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AeroMech.UI.Web.Services;

namespace AeroMech.UI.Web.Pages.Login
{
    public partial class Login
    {
        [Inject] NavigationManager _navigationManager { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        private Credential _credential = new Credential();
        private string _errorMessage = string.Empty;

        protected override void OnInitialized()
        {
            if (_navigationManager.Uri.Contains("login?Return"))
            {
                try
                {
                    _navigationManager.NavigateTo("/", forceLoad: true);
                }
                catch (Exception ex)
                {
                }
            }

            base.OnInitialized();
        }

        private async Task HandleSubmit()
        {
            _loaderService.ShowLoader();
            _errorMessage = string.Empty;

            try
            {
                // Use traditional window navigation for proper cookie handling
                var script = $@"
                    fetch('/Account/SignIn', {{
                        method: 'POST',
                        headers: {{ 'Content-Type': 'application/json' }},
                        body: JSON.stringify({{ userName: '{_credential.UserName}', password: '{_credential.Password}' }}),
                        credentials: 'same-origin'
                    }})
                    .then(response => response.json())
                    .then(data => {{
                        if (data.success) {{
                            window.location.href = '/';
                        }} else {{
                            console.error('Login failed:', data.message);
                        }}
                    }})
                    .catch(error => console.error('Error:', error));
                ";

                await JSRuntime.InvokeVoidAsync("eval", script);
            }
            catch (Exception ex)
            {
                _errorMessage = $"An error occurred during login: {ex.Message}";
                _loaderService.HideLoader();
            }
        }
    }
}

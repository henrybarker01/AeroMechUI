using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace AeroMech.UI.Web.Shared
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;
        [Inject] private UserService _userService { get; set; } = default!;
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;

        /// <summary>
        /// Whether the signed-in account still has to choose its own password. Read once per
        /// circuit - the flag only changes on this screen's own say-so, and changing it ends in a
        /// full reload that builds a fresh circuit anyway.
        /// </summary>
        private bool _mustChangePassword;

        protected override async Task OnInitializedAsync()
        {
            _navigationManager.LocationChanged += OnLocationChanged;

            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var userName = state.User?.Identity?.IsAuthenticated == true ? state.User.Identity.Name : null;

            if (string.IsNullOrWhiteSpace(userName))
            {
                return;
            }

            _mustChangePassword = await _userService.MustChangePassword(userName!);

            if (_mustChangePassword)
            {
                RedirectToChangePassword(_navigationManager.Uri);
            }
        }

        /// <summary>
        /// The layout outlives every internal navigation, so this is where an account that must
        /// still choose its own password is turned back from wherever else it was headed.
        /// </summary>
        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            if (_mustChangePassword)
            {
                RedirectToChangePassword(e.Location);
            }
        }

        private void RedirectToChangePassword(string currentUri)
        {
            var relative = _navigationManager.ToBaseRelativePath(currentUri);

            if (!relative.StartsWith("change-password", StringComparison.OrdinalIgnoreCase))
            {
                _navigationManager.NavigateTo("/change-password?forced=1");
            }
        }

        public void Dispose()
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}

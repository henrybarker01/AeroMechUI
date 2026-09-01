using Microsoft.AspNetCore.Components.Authorization;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// Who is signed in, for the audit trail to name.
    ///
    /// The name is read from the circuit's authentication state, which is where a Blazor screen's
    /// user lives, and falls back to the HTTP context for the few paths that run outside a circuit
    /// - a controller, a Razor page. Neither is guaranteed to be there, so a name is never assumed:
    /// an entry that cannot say who did something says so, rather than recording an empty user and
    /// reading as though nobody was involved.
    /// </summary>
    public class CurrentUserService
    {
        /// <summary>
        /// Written where the user genuinely could not be established. Spelled out rather than left
        /// blank so a reader can tell it apart from a column that was never filled in.
        /// </summary>
        public const string UnknownUser = "Unknown user";

        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(
            AuthenticationStateProvider authenticationStateProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _authenticationStateProvider = authenticationStateProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// The signed-in user's name, or <see cref="UnknownUser"/> where there is nobody to name.
        /// </summary>
        public async Task<string> GetUserName()
        {
            var fromCircuit = await FromAuthenticationState();

            if (!string.IsNullOrWhiteSpace(fromCircuit))
                return fromCircuit!;

            var fromRequest = _httpContextAccessor.HttpContext?.User?.Identity?.Name;

            return string.IsNullOrWhiteSpace(fromRequest) ? UnknownUser : fromRequest!;
        }

        /// <summary>
        /// The name a caller already holds, or the signed-in user if it holds none. Screens that
        /// ask who is doing the work - a stock take names who raised it - keep the name they were
        /// given, so the audit trail and the document agree on who was there.
        /// </summary>
        public async Task<string> GetUserName(string? preferredUserName)
            => string.IsNullOrWhiteSpace(preferredUserName)
                ? await GetUserName()
                : preferredUserName!.Trim();

        /// <summary>
        /// Outside a circuit the authentication state provider has nothing to hand back and says
        /// so by throwing. That is an ordinary case here, not a fault, so it is answered with
        /// "no name from this source" and the HTTP context is tried instead.
        /// </summary>
        private async Task<string?> FromAuthenticationState()
        {
            try
            {
                var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
                return state.User?.Identity?.Name;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}

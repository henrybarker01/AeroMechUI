using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// UI translation for the site. English text doubles as the lookup key, so a missing
    /// entry falls back to readable English rather than a bare resource name. Reports are
    /// deliberately not run through this service - they go out to clients in English.
    ///
    /// The chosen language lives in the browser twice: localStorage is the remembered
    /// preference, and a cookie carries it on the initial request so _Host can hand it to
    /// the circuit before anything renders. That matters because a Blazor circuit only
    /// re-renders components whose parameters changed - an in-place language flip leaves
    /// most of the page untouched - so the language is fixed per page load and switching
    /// reloads the page.
    /// </summary>
    public class LocalizationService
    {
        public const string English = "en";
        public const string Afrikaans = "af";

        private readonly IJSRuntime _js;
        private bool _checkedStorage;

        public LocalizationService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>Afrikaans until the cookie (via <see cref="SetInitialLanguage"/>) says otherwise.</summary>
        public string CurrentLanguage { get; private set; } = Afrikaans;

        public string this[string key] =>
            CurrentLanguage == Afrikaans && AfrikaansTranslations.Map.TryGetValue(key, out var translated)
                ? translated
                : key;

        /// <summary>For strings with placeholders, e.g. Format("Page {0} of {1}", page, pages).</summary>
        public string Format(string key, params object?[] args) => string.Format(this[key], args);

        /// <summary>
        /// Called from App.razor with the language _Host read from the cookie, before any
        /// component has rendered, so the whole first paint is already in the right language.
        /// </summary>
        public void SetInitialLanguage(string? language)
        {
            if (language == English || language == Afrikaans)
            {
                CurrentLanguage = language;
            }
        }

        /// <summary>
        /// After the first render (the earliest JS interop works): if localStorage remembers
        /// a different language than the cookie delivered - a pre-cookie preference, or the
        /// cookie was cleared - re-apply it and reload once. When they agree this is a no-op,
        /// so it cannot loop.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_checkedStorage)
            {
                return;
            }

            _checkedStorage = true;

            string? stored = null;
            try
            {
                stored = await _js.InvokeAsync<string?>("aeroMechStore.get", "aeromech-language");
            }
            catch
            {
                // Storage unavailable - stay on the language the cookie gave us.
            }

            if ((stored == English || stored == Afrikaans) && stored != CurrentLanguage)
            {
                await ApplyAndReloadAsync(stored);
            }
        }

        /// <summary>
        /// Persists the choice (localStorage + cookie) and reloads the page so every
        /// component - nav, layout, the page itself - comes back in the new language.
        /// </summary>
        public async Task SetLanguageAsync(string language)
        {
            if (language != English && language != Afrikaans || language == CurrentLanguage)
            {
                return;
            }

            await ApplyAndReloadAsync(language);
        }

        private async Task ApplyAndReloadAsync(string language)
        {
            try
            {
                await _js.InvokeVoidAsync("aeroMechLanguage.apply", language);
            }
            catch
            {
                // If the browser is already gone there is nothing to reload.
            }
        }
    }
}

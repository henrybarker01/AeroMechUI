using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace AeroMech.UI.Web.Pages.ListTemplate
{
    public partial class ListTemplate<TItem>
    {
        [Inject] private IJSRuntime JS { get; set; } = default!;

        [EditorRequired]
        [Parameter] public string Title { get; set; }

        [EditorRequired]
        [Parameter] public IEnumerable<TItem> Items { get; set; }

        [Parameter] public int PageSize { get; set; } = 15;

        [EditorRequired]
        [Parameter] public RenderFragment? HeaderTemplate { get; set; }

        [EditorRequired]
        [Parameter] public RenderFragment<TItem> RowTemplate { get; set; }

        [Parameter] public Func<TItem, string, bool>? SearchPredicate { get; set; }

        [Parameter] public EventCallback OnAdd { get; set; }

        [EditorRequired]
        [Parameter] public bool ShowAddButton { get; set; }

        /// <summary>
        /// Identifies this grid when the chosen sort order is remembered in the browser's local
        /// storage. Leave unset to keep the sort order for the lifetime of the page only.
        /// </summary>
        [Parameter] public string? StorageKey { get; set; }


        private string _searchTerm = string.Empty;
         
        [Parameter]
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm == value) return;
                _searchTerm = value ?? string.Empty;
                CurrentPage = 1;
            }
        }

        private int CurrentPage { get; set; } = 1;
        private int TotalPages => Math.Max(1, (int)Math.Ceiling((double)(FilteredItems?.Count() ?? 0) / PageSize));
        private bool IsFirstPage => CurrentPage == 1;
        private bool IsLastPage => CurrentPage >= TotalPages;

        // Selectors are supplied by the SortHeader cells in HeaderTemplate, which register
        // themselves as they render.
        private readonly Dictionary<string, Func<TItem, object?>> _sortSelectors = new(StringComparer.Ordinal);
        private string? _sortColumn;

        internal bool SortDescending { get; private set; }

        internal bool IsSortedBy(string column) => string.Equals(_sortColumn, column, StringComparison.Ordinal);

        internal void RegisterSortColumn(string column, Func<TItem, object?> selector)
        {
            var isNewColumn = !_sortSelectors.ContainsKey(column);
            _sortSelectors[column] = selector;

            // A stored sort order is read before the headers have registered, so the grid has to
            // render again once the column it names shows up.
            if (isNewColumn && IsSortedBy(column))
                StateHasChanged();
        }

        internal async Task ToggleSortAsync(string column)
        {
            if (IsSortedBy(column))
            {
                SortDescending = !SortDescending;
            }
            else
            {
                _sortColumn = column;
                SortDescending = false;
            }

            CurrentPage = 1;
            await SaveSortAsync();

            // The click was handled by the header cell, so the grid itself has to be told to redraw.
            StateHasChanged();
        }

        private IEnumerable<TItem> FilteredItems
        => (Items ?? Array.Empty<TItem>())
        .Where(i => string.IsNullOrWhiteSpace(SearchTerm) || (SearchPredicate?.Invoke(i, SearchTerm) ?? true));

        private IEnumerable<TItem> SortedItems
        {
            get
            {
                var items = FilteredItems;

                if (_sortColumn is null || !_sortSelectors.TryGetValue(_sortColumn, out var selector))
                    return items;

                return SortDescending
                    ? items.OrderByDescending(selector, GridSort.ValueComparer)
                    : items.OrderBy(selector, GridSort.ValueComparer);
            }
        }

        private IEnumerable<TItem> PagedItems
=> SortedItems
.Skip((CurrentPage - 1) * PageSize)
.Take(PageSize);

        private void NextPage()
        {
            if (IsLastPage) return;
            CurrentPage++;
            StateHasChanged();
        }
        private void PreviousPage()
        {
            if (IsFirstPage) return;
            CurrentPage--;
            StateHasChanged();
        }

        private string PreferenceKey => $"aeromech.grid.{StorageKey}.sort";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || string.IsNullOrWhiteSpace(StorageKey)) return;

            var stored = await LoadSortAsync();
            if (stored is null) return;

            _sortColumn = stored.Column;
            SortDescending = stored.Descending;
            StateHasChanged();
        }

        private async Task<GridSort.Preference?> LoadSortAsync()
        {
            try
            {
                var stored = await JS.InvokeAsync<string?>("aeroMechStore.get", PreferenceKey);
                return string.IsNullOrWhiteSpace(stored)
                    ? null
                    : JsonSerializer.Deserialize<GridSort.Preference>(stored);
            }
            catch (Exception ex) when (IsIgnorableStorageError(ex))
            {
                return null;
            }
        }

        private async Task SaveSortAsync()
        {
            if (string.IsNullOrWhiteSpace(StorageKey) || _sortColumn is null) return;

            try
            {
                var preference = new GridSort.Preference(_sortColumn, SortDescending);
                await JS.InvokeVoidAsync("aeroMechStore.set", PreferenceKey, JsonSerializer.Serialize(preference));
            }
            catch (Exception ex) when (IsIgnorableStorageError(ex))
            {
                // Remembering the sort order is a convenience; never fail the grid over it.
            }
        }

        private static bool IsIgnorableStorageError(Exception ex)
            => ex is JSException or JSDisconnectedException or JsonException or TaskCanceledException or OperationCanceledException;
    }
}

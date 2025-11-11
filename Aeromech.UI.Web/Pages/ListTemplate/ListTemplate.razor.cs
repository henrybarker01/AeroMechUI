using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.ListTemplate
{
    public partial class ListTemplate<TItem>
    {
        [Parameter] public string Title { get; set; } = "Items";
        [Parameter] public IEnumerable<TItem>? Items { get; set; }
        [Parameter] public int PageSize { get; set; } = 15;
        [Parameter] public RenderFragment? HeaderTemplate { get; set; }
        [Parameter] public RenderFragment<TItem>? RowTemplate { get; set; }
        [Parameter] public Func<TItem, string, bool>? SearchPredicate { get; set; }
        [Parameter] public EventCallback OnAdd { get; set; }
        [Parameter] public bool ShowAddButton { get; set; } = true;
        [Parameter] public bool IsLoading { get; set; } = false;


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

        private IEnumerable<TItem> FilteredItems
        => (Items ?? Array.Empty<TItem>())
        .Where(i => string.IsNullOrWhiteSpace(SearchTerm) || (SearchPredicate?.Invoke(i, SearchTerm) ?? true));

        private IEnumerable<TItem> PagedItems
=> FilteredItems
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
    }
}

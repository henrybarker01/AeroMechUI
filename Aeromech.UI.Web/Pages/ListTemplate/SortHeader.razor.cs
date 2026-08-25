using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.ListTemplate
{
    /// <summary>
    /// A header cell for <see cref="ListTemplate{TItem}"/> that orders the grid by
    /// <see cref="SortBy"/> when clicked. Without a selector it renders a plain header, so it can
    /// be used for action columns too.
    /// </summary>
    public partial class SortHeader<TItem>
    {
        [CascadingParameter] public ListTemplate<TItem>? Owner { get; set; }

        [Parameter] public string? Title { get; set; }

        /// <summary>The value this column orders on. Omit to render a header that cannot be sorted.</summary>
        [Parameter] public Func<TItem, object?>? SortBy { get; set; }

        /// <summary>
        /// Identifies the column in the stored sort order. Defaults to <see cref="Title"/>; set it
        /// explicitly when the title is likely to be reworded, or when two columns share a title.
        /// </summary>
        [Parameter] public string? SortKey { get; set; }

        [Parameter] public string? Class { get; set; }

        private string Column => string.IsNullOrWhiteSpace(SortKey) ? Title ?? string.Empty : SortKey;

        private bool IsSortable => SortBy is not null && Owner is not null && !string.IsNullOrWhiteSpace(Column);

        private bool IsActive => Owner?.IsSortedBy(Column) == true;

        private string IndicatorIcon => IsActive && Owner!.SortDescending ? "bi-caret-down-fill" : "bi-caret-up-fill";

        private string AriaSort => !IsActive ? "none" : Owner!.SortDescending ? "descending" : "ascending";

        protected override void OnParametersSet()
        {
            if (IsSortable)
                Owner!.RegisterSortColumn(Column, SortBy!);
        }

        private Task ToggleAsync() => Owner?.ToggleSortAsync(Column) ?? Task.CompletedTask;
    }
}

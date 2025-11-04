using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Components.PageLayout
{
    public partial class PageLayout
    {
        [Parameter] public string Title { get; set; } = "Title";
        [Parameter] public bool ShowAddButton { get; set; } = true;
        [Parameter] public EventCallback OnAddClicked { get; set; }
        [Parameter] public string SearchTerm { get; set; }
        [Parameter] public EventCallback<string> SearchTermChanged { get; set; }

        [Parameter] public int CurrentPage { get; set; }
        [Parameter] public int TotalPages { get; set; }
        [Parameter] public EventCallback<int> OnPageChanged { get; set; }

        [Parameter] public RenderFragment? HeaderContent { get; set; }
        [Parameter] public RenderFragment? ChildContent { get; set; }
    }
}

using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Components
{
    public partial class RowActions
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }
    }
}

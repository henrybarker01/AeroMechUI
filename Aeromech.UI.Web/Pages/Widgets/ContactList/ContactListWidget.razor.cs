using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.ContactList
{
    public partial class ContactListWidget
    {
        [Inject] ClientService ClientService { get; set; }
        // [Inject] NavigationManager NavigationManager { get; set; }

        List<ClientModel> clients = new List<ClientModel>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                clients = await ClientService.GetClients();
                await InvokeAsync(StateHasChanged);
            }
        }

        //private void PrintQuote(int Id)
        //{
        //  //  NavigationManager.NavigateTo($"/ShowQuote/{Id}");
        //}

        //private void EditQuote(int serviceReportId)
        //{
        //    NavigationManager.NavigateTo($"/add-service-report/{serviceReportId}");
        //}
    }
}
using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.ContactList
{
    public partial class ContactListWidget
    {
        [Inject] ClientService ClientService { get; set; }
      // [Inject] NavigationManager NavigationManager { get; set; }

        List<ClientModel> clients { get; set; }

        protected override async Task OnInitializedAsync()
        {
            clients = await ClientService.GetClients();
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
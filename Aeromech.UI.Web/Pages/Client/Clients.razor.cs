using AeroMech.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Client
{
    public partial class Clients
    {
        [Inject] ClientService clientService { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] protected ConfirmationService _confirmationService { get; set; }

        private string title = string.Empty;
        private Modal modal = default!;
        private ClientModel client = new ClientModel();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GetClients();
            }
        }

        //TODO - HB - This really is done simpleton way
        private ClientRateModel clientRatesWeekdays = new ClientRateModel()
        {
            RateType = Models.Enums.RateType.Weekdays
        };
        private ClientRateModel clientRatesWeekdaysOvertime = new ClientRateModel()
        {
            RateType = Models.Enums.RateType.WeekdaysOvertime
        };
        private ClientRateModel clientRatesOvertime = new ClientRateModel()
        {
            RateType = Models.Enums.RateType.Overtime
        };
        private ClientRateModel clientRatesSundaysAndPublicHolidays = new ClientRateModel()
        {
            RateType = Models.Enums.RateType.SundaysAndPublicHolidays
        };

        private List<ClientModel>? clients = new List<ClientModel>();

        private bool MatchesSearch(ClientModel client, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();

            return
                (client.Name ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (client.ContactPersonName ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase);
        }

        private async Task OnShowModalClick()
        {
            title = "Add Client";
            client = new ClientModel();
            await modal.ShowAsync();
        }

        private async Task OnEditClientClick(ClientModel clientModel)
        {
            title = "Edit Client";
            client = clientModel;

            clientRatesOvertime = client.Rates?.FirstOrDefault(x => x.RateType == Models.Enums.RateType.Overtime) ?? new ClientRateModel() { RateType = Models.Enums.RateType.Overtime };
            clientRatesWeekdaysOvertime = client.Rates?.FirstOrDefault(x => x.RateType == Models.Enums.RateType.WeekdaysOvertime) ?? new ClientRateModel() { RateType = Models.Enums.RateType.WeekdaysOvertime };
            clientRatesWeekdays = client.Rates?.FirstOrDefault(x => x.RateType == Models.Enums.RateType.Weekdays) ?? new ClientRateModel() { RateType = Models.Enums.RateType.Weekdays };
            clientRatesSundaysAndPublicHolidays = client.Rates?.FirstOrDefault(x => x.RateType == Models.Enums.RateType.SundaysAndPublicHolidays) ?? new ClientRateModel() { RateType = Models.Enums.RateType.SundaysAndPublicHolidays };

            await modal.ShowAsync();
        }

        private async Task OnHideModalClick()
        {
            await GetClients();
            StateHasChanged();
            await modal.HideAsync();
        }

        private async void AddNewClient()
        {
            if (client.Id == 0)
            {
                client.Rates =
                [
                    clientRatesOvertime,
                    clientRatesWeekdaysOvertime,
                    clientRatesWeekdays,
                    clientRatesSundaysAndPublicHolidays,
                ];

                _loaderService.ShowLoader();
                var result = await clientService.AddClient(client);
                _loaderService.HideLoader();
                if (result != 0)
                {
                    await OnHideModalClick();
                }
            }
            else
            {
                if (client.Rates != null && client.Rates.Count > 0)
                {
                    client.Rates.FirstOrDefault(x => x.RateType == Models.Enums.RateType.Overtime).Rate = clientRatesOvertime.Rate;
                    client.Rates.FirstOrDefault(x => x.RateType == Models.Enums.RateType.WeekdaysOvertime).Rate = clientRatesWeekdaysOvertime.Rate;
                    client.Rates.FirstOrDefault(x => x.RateType == Models.Enums.RateType.Weekdays).Rate = clientRatesWeekdays.Rate;
                    client.Rates.FirstOrDefault(x => x.RateType == Models.Enums.RateType.SundaysAndPublicHolidays).Rate = clientRatesSundaysAndPublicHolidays.Rate;
                }
                else
                {
                    client.Rates =
                    [
                        clientRatesOvertime,
                        clientRatesWeekdaysOvertime,
                        clientRatesWeekdays,
                        clientRatesSundaysAndPublicHolidays,
                    ];
                }

                _loaderService.ShowLoader();
                var result = await clientService.EditClient(client);
                _loaderService.HideLoader();
                if (result != 0)
                {
                    await OnHideModalClick();
                }
            }
        }

        private async Task GetClients()
        {
            _loaderService.ShowLoader();
            clients = await clientService.GetClients();
            _loaderService.HideLoader();
            await InvokeAsync(StateHasChanged);
        }

        private async Task DeleteClient(AeroMech.Models.ClientModel client)
        {
            bool confirmed = await _confirmationService.ConfirmAsync("Are you sure?");
            if (confirmed)
            {
                _loaderService.ShowLoader();
                await clientService.Delete(client.Id);
                clients?.Remove(client);
                _loaderService.HideLoader();
            }
        }
    }
}
using AeroMech.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Client
{
    public partial class Clients
    {
        [Inject] private ClientService clientService { get; set; }
        [Inject] private LoaderService _loaderService { get; set; }
        [Inject] private ConfirmationService _confirmationService { get; set; }

        private string _title = string.Empty;
        private Modal _modal = default!;
        private ClientModel _client = new ClientModel();

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
            _title = "Add Client";
            _client = new ClientModel();
            await _modal.ShowAsync();
        }

        private async Task OnEditClientClick(ClientModel clientModel)
        {
            _title = "Edit Client";
            _client = clientModel;

            clientRatesOvertime = _client.Rates?.FirstOrDefault(x => x.RateType == Models.Enums.RateType.Overtime) ?? new ClientRateModel() { RateType = Models.Enums.RateType.Overtime };
            clientRatesWeekdaysOvertime = _client.Rates?.FirstOrDefault(x => x.RateType == Models.Enums.RateType.WeekdaysOvertime) ?? new ClientRateModel() { RateType = Models.Enums.RateType.WeekdaysOvertime };
            clientRatesWeekdays = _client.Rates?.FirstOrDefault(x => x.RateType == Models.Enums.RateType.Weekdays) ?? new ClientRateModel() { RateType = Models.Enums.RateType.Weekdays };
            clientRatesSundaysAndPublicHolidays = _client.Rates?.FirstOrDefault(x => x.RateType == Models.Enums.RateType.SundaysAndPublicHolidays) ?? new ClientRateModel() { RateType = Models.Enums.RateType.SundaysAndPublicHolidays };

            await _modal.ShowAsync();
        }

        private async Task OnHideModalClick()
        {
            await GetClients();
            StateHasChanged();
            await _modal.HideAsync();
        }

        private async void AddNewClient()
        {
            if (_client.Id == 0)
            {
                _client.Rates =
                [
                    clientRatesOvertime,
                    clientRatesWeekdaysOvertime,
                    clientRatesWeekdays,
                    clientRatesSundaysAndPublicHolidays,
                ];

                _loaderService.ShowLoader();
                var result = await clientService.AddClient(_client);
                _loaderService.HideLoader();
                if (result != 0)
                {
                    await OnHideModalClick();
                }
            }
            else
            {
                if (_client.Rates != null && _client.Rates.Count > 0)
                {
                    _client.Rates.FirstOrDefault(x => x.RateType == Models.Enums.RateType.Overtime).Rate = clientRatesOvertime.Rate;
                    _client.Rates.FirstOrDefault(x => x.RateType == Models.Enums.RateType.WeekdaysOvertime).Rate = clientRatesWeekdaysOvertime.Rate;
                    _client.Rates.FirstOrDefault(x => x.RateType == Models.Enums.RateType.Weekdays).Rate = clientRatesWeekdays.Rate;
                    _client.Rates.FirstOrDefault(x => x.RateType == Models.Enums.RateType.SundaysAndPublicHolidays).Rate = clientRatesSundaysAndPublicHolidays.Rate;
                }
                else
                {
                    _client.Rates =
                    [
                        clientRatesOvertime,
                        clientRatesWeekdaysOvertime,
                        clientRatesWeekdays,
                        clientRatesSundaysAndPublicHolidays,
                    ];
                }

                _loaderService.ShowLoader();
                var result = await clientService.EditClient(_client);
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
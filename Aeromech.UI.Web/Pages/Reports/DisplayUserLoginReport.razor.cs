using AeroMech.Data.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    /// <summary>
    /// The sign-in traffic as a report: who signed in, who signed out, and whose attempts were
    /// refused. Filtered the way that question is actually asked - a period first, then a person
    /// or the kind of event being chased.
    /// </summary>
    public partial class DisplayUserLoginReport
    {
        [Inject] private AuditReportService AuditReportService { get; set; } = default!;
        [Inject] private LoaderService LoaderService { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private readonly UserLoginReportRequestModel _request = new();

        private List<string> _users = new();
        private readonly HashSet<string> _selectedUsers = new(StringComparer.OrdinalIgnoreCase);

        private readonly List<AuditAction> _events = new()
        {
            AuditAction.LoggedIn,
            AuditAction.LoginFailed,
            AuditAction.LoggedOut
        };

        private readonly HashSet<AuditAction> _selectedEvents = new();

        private string? _pdfBase64String;
        private byte[]? _pdfBytes;
        private int _reportVersion;

        private string SelectedUsersLabel => _selectedUsers.Count switch
        {
            0 => "All users",
            1 => _selectedUsers.First(),
            _ => $"{_selectedUsers.Count} users selected"
        };

        private string SelectedEventsLabel => _selectedEvents.Count switch
        {
            0 => "All sign-in activity",
            1 => DescribeEvent(_selectedEvents.First()),
            _ => $"{_selectedEvents.Count} selected"
        };

        /// <summary>
        /// The same wording the report prints, so the filter and the document it produces name an
        /// event identically.
        /// </summary>
        private static string DescribeEvent(AuditAction action) => Services.AuditReportService.Describe(action);

        protected override async Task OnInitializedAsync()
        {
            _users = await AuditReportService.GetLoginUsers();
        }

        private void SetPeriod(int period)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            switch (period)
            {
                case 0:
                    _request.FromDate = today;
                    _request.ToDate = today;
                    break;

                case 1:
                    _request.FromDate = today.AddDays(-6);
                    _request.ToDate = today;
                    break;

                case 2:
                    _request.FromDate = new DateOnly(today.Year, today.Month, 1);
                    _request.ToDate = today;
                    break;

                default:
                    _request.FromDate = today.AddMonths(-3);
                    _request.ToDate = today;
                    break;
            }

            ClearReport();
        }

        private void OnFromDateChanged(DateOnly value)
        {
            _request.FromDate = value;
            ClearReport();
        }

        private void OnToDateChanged(DateOnly value)
        {
            _request.ToDate = value;
            ClearReport();
        }

        private void ToggleUser(string userName, bool isSelected)
        {
            if (isSelected)
                _selectedUsers.Add(userName);
            else
                _selectedUsers.Remove(userName);
        }

        private void ClearUserSelection() => _selectedUsers.Clear();

        private void ToggleEvent(AuditAction loginEvent, bool isSelected)
        {
            if (isSelected)
                _selectedEvents.Add(loginEvent);
            else
                _selectedEvents.Remove(loginEvent);
        }

        private void ClearEventSelection() => _selectedEvents.Clear();

        private void ClearReport()
        {
            _pdfBytes = null;
            _pdfBase64String = null;
        }

        private async Task ViewReport()
        {
            LoaderService.ShowLoader();
            try
            {
                _request.UserNames = _selectedUsers.ToList();
                _request.Events = _selectedEvents.ToList();

                _pdfBytes = await AuditReportService.GenerateUserLoginReport(_request);
                _pdfBase64String = Convert.ToBase64String(_pdfBytes);
                _reportVersion++;
            }
            catch (InvalidOperationException ex)
            {
                ClearReport();
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
            catch (Exception)
            {
                ClearReport();
                ToastService.Notify(new(ToastType.Danger, "The user login report could not be generated."));
            }
            finally
            {
                LoaderService.HideLoader();
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task DownloadPdf()
        {
            if (_pdfBytes is null)
                return;

            var fileName = $"UserLogins_{_request.FromDate:yyyyMMdd}_{_request.ToDate:yyyyMMdd}.pdf";
            await DownloadFileFromStream(_pdfBytes, fileName);
        }

        private async Task DownloadFileFromStream(byte[] fileBytes, string fileName)
        {
            var fileStream = new MemoryStream(fileBytes);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}

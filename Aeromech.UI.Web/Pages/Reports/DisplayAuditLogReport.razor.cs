using AeroMech.Data.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    /// <summary>
    /// The audit trail as a report: who changed what, when, and what the value was before they
    /// changed it. Filtered the way the log is actually questioned - a period first, then a
    /// person, a subject, or the part or invoice being asked about.
    /// </summary>
    public partial class DisplayAuditLogReport
    {
        [Inject] private AuditReportService AuditReportService { get; set; } = default!;
        [Inject] private LoaderService LoaderService { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private readonly AuditLogReportRequestModel _request = new();

        private List<string> _users = new();
        private readonly HashSet<string> _selectedUsers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Every area except <see cref="AuditArea.None"/>, which is never written and would only
        /// offer a filter that matches nothing.
        /// </summary>
        private readonly List<AuditArea> _areas = Enum.GetValues<AuditArea>()
            .Where(x => x != AuditArea.None)
            .ToList();

        private readonly HashSet<AuditArea> _selectedAreas = new();

        private string? _pdfBase64String;
        private byte[]? _pdfBytes;
        private int _reportVersion;

        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        private string SelectedUsersLabel => _selectedUsers.Count switch
        {
            0 => "All users",
            1 => _selectedUsers.First(),
            _ => $"{_selectedUsers.Count} users selected"
        };

        private string SelectedAreasLabel => _selectedAreas.Count switch
        {
            0 => "All activity",
            1 => DescribeArea(_selectedAreas.First()),
            _ => $"{_selectedAreas.Count} selected"
        };

        /// <summary>
        /// The same wording the report prints, so the filter and the document it produces name an
        /// area identically. Wrapped rather than called directly because the injected service
        /// shares its name with the type the description lives on.
        /// </summary>
        private static string DescribeArea(AuditArea area) => Services.AuditReportService.Describe(area);

        protected override async Task OnInitializedAsync()
        {
            _users = await AuditReportService.GetUsers();
        }

        /// <summary>
        /// The periods actually asked for, rather than making somebody pick two dates to answer
        /// "what happened today". Clearing the report with them, because the dates on screen would
        /// otherwise describe a document built for a different period.
        /// </summary>
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

        private void ToggleArea(AuditArea area, bool isSelected)
        {
            if (isSelected)
                _selectedAreas.Add(area);
            else
                _selectedAreas.Remove(area);
        }

        private void ClearAreaSelection() => _selectedAreas.Clear();

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
                _request.Areas = _selectedAreas.ToList();

                _pdfBytes = await AuditReportService.GenerateAuditLogReport(_request);
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
                ToastService.Notify(new(ToastType.Danger, "The audit log report could not be generated."));
            }
            finally
            {
                LoaderService.HideLoader();
            }

            await InvokeAsync(StateHasChanged);
        }

        private void OnDocumentLoaded(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnDocumentLoaded, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private void OnPageChanged(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnPageChanged, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private async Task DownloadPdf()
        {
            if (_pdfBytes is null)
                return;

            var fileName = $"AuditLog_{_request.FromDate:yyyyMMdd}_{_request.ToDate:yyyyMMdd}.pdf";
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

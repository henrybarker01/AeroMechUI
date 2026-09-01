using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    /// <summary>
    /// The service history of a machine, or of every machine of a type.
    ///
    /// One page rather than two, because the only difference between the two reports is the
    /// heading the same service reports are gathered under, and a reader who opened the wrong one
    /// wants to switch rather than to go back. The reports view links straight to the grouping it
    /// names, so arriving from either card lands on the right view.
    /// </summary>
    public partial class DisplayVehicleServiceRecordReport
    {
        [Inject] private VehicleReportService VehicleReportService { get; set; } = default!;
        [Inject] private LoaderService LoaderService { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        /// <summary>
        /// The grouping the page was opened on, as it appears in the route.
        /// </summary>
        public const string ByTypeRouteSegment = "by-type";

        [Parameter] public string? Mode { get; set; }

        private readonly VehicleServiceRecordReportRequestModel _request = new();

        private List<VehicleOptionModel> _vehicles = new();
        private List<string> _machineTypes = new();

        private readonly HashSet<int> _selectedVehicleIds = new();
        private readonly HashSet<string> _selectedTypes = new(StringComparer.OrdinalIgnoreCase);

        private string? _pdfBase64String;
        private byte[]? _pdfBytes;
        private int _reportVersion;

        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        private bool IsByType => _request.Grouping == VehicleServiceRecordGrouping.ByMachineType;

        private string Heading => IsByType ? "Vehicle Type Service Record" : "Vehicle Service Record";

        private string Blurb => IsByType
            ? "Every service recorded against machines of a type, so a fault that keeps coming back on one model can be seen."
            : "Everything that has been done to a machine, in the order it happened.";

        private string SelectedVehiclesLabel => _selectedVehicleIds.Count switch
        {
            0 => "All machines",
            1 => _vehicles.FirstOrDefault(x => x.Id == _selectedVehicleIds.First())?.Label ?? "1 machine selected",
            _ => $"{_selectedVehicleIds.Count} machines selected"
        };

        private string SelectedTypesLabel => _selectedTypes.Count switch
        {
            0 => "All machine types",
            1 => _selectedTypes.First(),
            _ => $"{_selectedTypes.Count} machine types selected"
        };

        protected override async Task OnInitializedAsync()
        {
            _vehicles = await VehicleReportService.GetVehicleOptions();
            _machineTypes = await VehicleReportService.GetMachineTypes();
        }

        // Runs on every navigation rather than only on first load, so following the other card
        // from the reports view re-reads the grouping instead of keeping the one already on screen.
        protected override void OnParametersSet()
            => _request.Grouping = string.Equals(Mode, ByTypeRouteSegment, StringComparison.OrdinalIgnoreCase)
                ? VehicleServiceRecordGrouping.ByMachineType
                : VehicleServiceRecordGrouping.ByVehicle;

        private void SetGrouping(VehicleServiceRecordGrouping grouping)
        {
            if (_request.Grouping == grouping)
                return;

            _request.Grouping = grouping;

            // The document on screen was gathered under the heading that has just been left, and
            // the filter it was built from no longer applies either.
            ClearReport();
        }

        private void ToggleVehicle(int vehicleId, bool isSelected)
        {
            if (isSelected)
                _selectedVehicleIds.Add(vehicleId);
            else
                _selectedVehicleIds.Remove(vehicleId);
        }

        private void ClearVehicleSelection() => _selectedVehicleIds.Clear();

        private void ToggleMachineType(string machineType, bool isSelected)
        {
            if (isSelected)
                _selectedTypes.Add(machineType);
            else
                _selectedTypes.Remove(machineType);
        }

        private void ClearTypeSelection() => _selectedTypes.Clear();

        // An emptied date box means the period is open at that end, which is not the same as an
        // unreadable one - both land here, and both leave the bound unset.
        private void OnFromDateChanged(ChangeEventArgs args)
            => _request.FromDate = ParseDate(args.Value?.ToString());

        private void OnToDateChanged(ChangeEventArgs args)
            => _request.ToDate = ParseDate(args.Value?.ToString());

        private static DateOnly? ParseDate(string? value)
            => DateOnly.TryParse(value, out var date) ? date : null;

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
                // Only the filter belonging to the grouping on screen is sent: a machine picked
                // before switching to types would otherwise quietly narrow the type report.
                _request.VehicleIds = IsByType ? new List<int>() : _selectedVehicleIds.ToList();
                _request.MachineTypes = IsByType ? _selectedTypes.ToList() : new List<string>();

                _pdfBytes = await VehicleReportService.GenerateVehicleServiceRecordReport(_request);
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
                ToastService.Notify(new(ToastType.Danger, "The service record could not be generated."));
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

            var prefix = IsByType ? "MachineTypeServiceRecord" : "VehicleServiceRecord";
            await DownloadFileFromStream(_pdfBytes, $"{prefix}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        private async Task DownloadFileFromStream(byte[] fileBytes, string fileName)
        {
            var fileStream = new MemoryStream(fileBytes);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}

using AeroMech.Data.Models;
using AeroMech.Models;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Pages.Employee;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;

namespace AeroMech.UI.Web.Pages.ServiceReport
{
    public partial class AddServiceReport
    {
        [Inject] ClientService ClientService { get; set; }
        [Inject] EmployeeService EmployeeService { get; set; }
        [Inject] PartsService PartsService { get; set; }
        [Inject] VehicleService VehicleService { get; set; }
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] QuoteService QuoteService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] protected BlazorBootstrap.ToastService ToastService { get; set; }

        [Parameter] public int serviceReportId { get; set; }

        /// <summary>
        /// Set when the page was reached from an accepted quote, which opens the form pre-filled
        /// with the quoted work and an empty labour grid.
        /// </summary>
        [Parameter] public int quoteId { get; set; }

        private bool IsConversion => _serviceReport.Id == 0 && _serviceReport.QuoteId.HasValue;

        /// <summary>
        /// The quote being converted, kept so the screen can show what was quoted beside what is
        /// being captured. Null on an ordinary service report.
        /// </summary>
        private QuoteModel? _sourceQuote;

        /// <summary>
        /// Set by the user to say the difference between quoted and captured labour is deliberate.
        /// The quote is an estimate, so the captured hours win - but not silently.
        /// </summary>
        private bool _labourVarianceAccepted;

        /// <summary>
        /// One rate type, with what the quote priced against what has been captured. Quoting is
        /// per rate type, so this is the level the two sides can actually be tied together at -
        /// equal totals can still hide weekday hours booked as overtime.
        /// </summary>
        private sealed record LabourReconciliationLine(
            RateType RateType, double Rate, double QuotedHours, double CapturedHours)
        {
            public double Variance => CapturedHours - QuotedHours;

            // Hours are entered to two decimals, so compare at that precision rather than exactly.
            public bool Matches => Math.Abs(Variance) < 0.005;
        }

        private List<LabourReconciliationLine> LabourReconciliation
        {
            get
            {
                var quoted = (_sourceQuote?.Labour ?? new())
                    .Where(x => !x.IsDeleted)
                    .GroupBy(x => x.RateType)
                    .ToDictionary(g => g.Key, g => (Hours: g.Sum(x => x.Hours), Rate: g.First().Rate));

                var captured = (_serviceReport.Employees ?? new())
                    .Where(x => !x.IsDeleted)
                    .GroupBy(x => x.RateType)
                    .ToDictionary(g => g.Key, g => (Hours: g.Sum(x => x.Hours ?? 0), Rate: g.First().Rate));

                return quoted.Keys.Union(captured.Keys)
                    .Select(rateType =>
                    {
                        var q = quoted.TryGetValue(rateType, out var qv) ? qv : default;
                        var c = captured.TryGetValue(rateType, out var cv) ? cv : default;

                        // A rate the quote priced keeps the quoted rate; one that only appears in
                        // the captured labour is shown at the rate it was captured at.
                        return new LabourReconciliationLine(
                            rateType,
                            q.Rate > 0 ? q.Rate : c.Rate,
                            q.Hours,
                            c.Hours);
                    })
                    // A freshly added employee sits on RateType.None with no hours until it is
                    // filled in; that is not a discrepancy worth showing.
                    .Where(line => line.QuotedHours > 0 || line.CapturedHours > 0)
                    .OrderBy(line => line.RateType.ToString(), StringComparer.Ordinal)
                    .ToList();
            }
        }

        private double QuotedHours => _sourceQuote?.Labour.Where(x => !x.IsDeleted).Sum(x => x.Hours) ?? 0;

        private double QuotedLabourValue => _sourceQuote?.Labour.Where(x => !x.IsDeleted)
            .Sum(x => x.Rate * x.Hours - x.Discount / 100 * (x.Rate * x.Hours)) ?? 0;

        private double CapturedHours => _serviceReport.Employees?
            .Where(x => !x.IsDeleted).Sum(x => x.Hours ?? 0) ?? 0;

        private double CapturedLabourValue => _serviceReport.Employees?
            .Where(x => !x.IsDeleted).Sum(x => getTotal(x)) ?? 0;

        private double HoursVariance => CapturedHours - QuotedHours;

        // Every rate type has to tie back, not just the totals.
        private bool LabourMatchesQuote => LabourReconciliation.All(line => line.Matches);

        private string LabourVarianceSummary
        {
            get
            {
                var off = LabourReconciliation.Where(line => !line.Matches).ToList();

                if (off.Count == 0) return string.Empty;

                return string.Join(", ", off.Select(line =>
                    $"{line.RateType.GetDisplayName()} {Math.Abs(line.Variance):0.##} h " +
                    $"{(line.Variance > 0 ? "over" : "under")}"));
            }
        }

        private EditContext? editContext;
        private BlazorBootstrap.Modal salesOrderNumberModal = default!;

        private ServiceReportModel _serviceReport;
        List<EmployeeModel> employees = new List<EmployeeModel>();
        List<ClientModel> clients = new List<ClientModel>();
        List<VehicleModel> vehicles = new List<VehicleModel>();
        List<PartModel> parts = new List<PartModel>();
        ServiceReportPartModel selectedPart = new ServiceReportPartModel();
        ServiceReportEmployeeModel selectedEmployee = new ServiceReportEmployeeModel();

        private IEnumerable<RateType> RateTypes => Enum.GetValues<RateType>();

        protected override void OnInitialized()
        {
            _serviceReport = new ServiceReportModel();
            editContext = new(_serviceReport);

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _loaderService.ShowLoader();

                employees = await EmployeeService.GetEmployees();
                clients = await ClientService.GetClients();
                parts = await PartsService.GetParts();

                if (serviceReportId != 0)
                {
                    // pageTitle = "Edit Field Service Report";
                    await LoadServiceReport(serviceReportId);
                }
                else if (quoteId != 0)
                {
                    await LoadFromQuote(quoteId);
                }
                else
                {
                    // pageTitle = "New Field Service Report";
                    InitServiceReport();
                }
                await InvokeAsync(StateHasChanged);
                _loaderService.HideLoader();
            }
        }

        private async Task LoadServiceReport(int id)
        {
            _serviceReport = await ServiceReportService.GetServiceReport(id);

            if (_serviceReport.ClientId != 0)
            {
                var vehicleId = _serviceReport.VehicleId;
                await HandleOnChangeClient(_serviceReport.ClientId);

                if (vehicleId != 0)
                {
                    HandleOnChangeVehicle(vehicleId);
                }
            }

            editContext = new(_serviceReport);

            // _serviceReport is a new instance, so the grids must rebind to it.
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Opens the form on an accepted quote: the quoted parts, client, vehicle and scope come
        /// across, but the labour does not. Quoted labour was priced per rate type and says
        /// nothing about who did the work, so the user enters the actual hours per person here.
        /// </summary>
        private async Task LoadFromQuote(int id)
        {
            try
            {
                _sourceQuote = await QuoteService.GetQuote(id);
                _serviceReport = QuoteService.BuildServiceReportFromQuote(_sourceQuote);
            }
            catch (Exception ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
                NavigationManager.NavigateTo("/quotes");
                return;
            }

            if (_serviceReport.ClientId != 0)
            {
                // HandleOnChangeClient clears the vehicle as it reloads the client's list, so the
                // quoted vehicle is put back afterwards.
                var vehicleId = _serviceReport.VehicleId;
                await HandleOnChangeClient(_serviceReport.ClientId);

                if (vehicleId != 0)
                {
                    HandleOnChangeVehicle(vehicleId);
                }
            }

            editContext = new(_serviceReport);

            await InvokeAsync(StateHasChanged);
        }

        private void InitServiceReport()
        {
            _serviceReport.Employees = new List<ServiceReportEmployeeModel>();

            _serviceReport.Parts = new List<ServiceReportPartModel>();

            _serviceReport.ReportDate = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero); 

            StateHasChanged();
        }

        private void RemoveLabour(int Id)
        {
            var employee = _serviceReport.Employees.SingleOrDefault(x => x.Id == Id);
            if (employee != null)
                employee.IsDeleted = true;
        }

        private void RemovePart(int Id)
        {
            var part = _serviceReport.Parts.SingleOrDefault(x => x.Id == Id);
            if (part != null)
                part.IsDeleted = true;
        }

        private double getTotal(ServiceReportEmployeeModel employee)
        {
            var t = ((employee.Rate * employee.Hours) - ((employee.Discount ?? 0 / 100) * (employee.Rate * employee.Hours))) ?? 0;
            return t;
        }

        private double getRate(ServiceReportEmployeeModel employee)
        {
            if (employee.RateType == RateType.None || _serviceReport.ClientId == 0) return 0;

            var clientRate = _serviceReport.Client?.Rates?.SingleOrDefault(x => x.RateType == employee.RateType);
            employee.RateType = clientRate?.RateType ?? employee.RateType;
            employee.Rate = employee.Rate > 0 ? employee.Rate : clientRate?.Rate ?? 0;
            return employee.Rate;
        }

        //void HandleOnRateChangeUnbound(int rateType, int employeeId)
        //{
        //    var employee = serviceReport.Employees.Single(x => x.Id == employeeId);
        //    employee.RateType = (RateType)rateType;
        //}
        void HandleOnChangeUnbound(int employeeId)
        {
            var emp = employees.FirstOrDefault(x => x.Id == employeeId);
            if (emp == null) return;

            var employee = new ServiceReportEmployeeModel
            {
                FirstName = emp.FirstName,
                LastName = emp.LastName,
                EmployeeId = emp.Id,
                BirthDate = emp.BirthDate,
                Email = emp.Email,
                City = emp.City,
                AddressId = emp.AddressId,
                AddressLine1 = emp.AddressLine1,
                AddressLine2 = emp.AddressLine2,
                IDNumber = emp.IDNumber,
                PhoneNumber = emp.PhoneNumber,
                PostalCode = emp.PostalCode,
                Title = emp.Title,
                DutyDate = DateOnly.FromDateTime(DateTime.Now)
            };

            _serviceReport.Employees.Add(employee);
            selectedEmployee = new ServiceReportEmployeeModel();
        }

        private async Task HandleOnChangeClient(int clientId)
        {
            _serviceReport.ClientId = clientId;
            _serviceReport.Client = clients.SingleOrDefault(x => x.Id == clientId);
            _serviceReport.VehicleId = 0;
            vehicles = await VehicleService.GetVehicles(clientId);
            
            if (_serviceReport.Client == null)
            {
                _serviceReport.Client = new ClientModel();
            }

            StateHasChanged();
        }

        /// <summary>
        /// Validation that the user has to be told about. A plain save is deliberately permissive
        /// so an incomplete report can be kept as a draft, but printing is not: the PDF reads
        /// straight off the client and vehicle. Either way, refusing has to say why - silently
        /// doing nothing reads as a broken button.
        /// </summary>
        private bool ValidateForPrint()
        {
            if (editContext.Validate()) return true;

            var problems = editContext.GetValidationMessages().Distinct().Take(3).ToList();

            ToastService.Notify(new(ToastType.Danger, problems.Count > 0
                ? $"Cannot print yet: {string.Join(" ", problems)}"
                : "Cannot print yet: the report is not complete."));

            return false;
        }

        private async Task Save()
        {
            var serviceReportId = await SaveServiceReport(_serviceReport);
            if (serviceReportId == 0) return;

            _serviceReport.Id = serviceReportId;
            ToastService.Notify(new(ToastType.Success, "Service report saved successfully."));
        }

        private async Task SaveAndNew()
        {
            var result = await SaveServiceReport(_serviceReport);
            if (result == 0) return;

            _serviceReport = new ServiceReportModel();
            // The grids and validation both bind through the edit context, so it has to follow
            // the new model rather than keep validating the one just saved.
            editContext = new(_serviceReport);
            _sourceQuote = null;
            _labourVarianceAccepted = false;
            InitServiceReport();

            ToastService.Notify(new(ToastType.Success, "Service report saved successfully."));
        }

        private async Task SaveAndGenerateServiceReport()
        {
            await salesOrderNumberModal.HideAsync();

            if (!ValidateForPrint()) return;

            var serviceReportId = await SaveServiceReport(_serviceReport);
            if (serviceReportId == 0) return;

            _serviceReport.Id = serviceReportId;
            ToastService.Notify(new(ToastType.Success, "Service report saved successfully."));
            NavigationManager.NavigateTo($"/ShowPDF/{serviceReportId}");
        }

        private string SearchTerm { get; set; } = string.Empty;
        private IEnumerable<PartModel> FilteredParts =>
            parts.Where(p => string.IsNullOrEmpty(SearchTerm) ||
                             p.PartCode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                             p.PartDescription.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

        private async Task<int> SaveServiceReport(ServiceReportModel serviceReport)
        {
            // Converting a quote is only worth anything once the real labour is on it, so the
            // save is refused until at least one person has hours against them.
            if (IsConversion && !serviceReport.Employees.Any(x => !x.IsDeleted && x.Hours > 0))
            {
                ToastService.Notify(new(ToastType.Danger, "Enter the labour that was actually worked before converting this quote."));
                return 0;
            }

            // The captured hours are what gets invoiced and what reaches the timesheets, so they
            // are allowed to differ from the quote - but the user has to say so, otherwise a
            // mistyped figure converts silently.
            if (IsConversion && CapturedHours > 0 && !LabourMatchesQuote && !_labourVarianceAccepted)
            {
                ToastService.Notify(new(ToastType.Warning,
                    $"Captured labour does not tie back to the quote: {LabourVarianceSummary}. " +
                    "Tick the confirmation below to convert with the difference."));
                return 0;
            }

            var isValid = editContext.Validate();
            if (isValid)
            {
                _serviceReport.IsComplete = _serviceReport.SalesOrderNumber != null;
            }

            serviceReport.Description = "Description";

            int savedId;

            // A save that throws used to disappear into an async void handler and look to the
            // user like the button had done nothing at all.
            try
            {
                savedId = serviceReport.Id == 0
                    ? await ServiceReportService.AddServiceReport(serviceReport)
                    : await ServiceReportService.EditServiceReport(serviceReport);
            }
            catch (Exception ex)
            {
                ToastService.Notify(new(ToastType.Danger, $"Service report could not be saved. {ex.Message}"));
                return 0;
            }

            if (savedId == 0)
            {
                ToastService.Notify(new(ToastType.Danger, "Service report could not be saved."));
            }
            else
            {
                // Reload so labour and parts carry their database ids. Without them the
                // next save cannot tell saved rows apart from new ones and inserts duplicates.
                await LoadServiceReport(savedId);
            }

            return savedId;
        }

        private void HandleOnServiceTypeChange(ServiceType serviceType)
        {
            _serviceReport.ServiceType = serviceType;
        }

        void HandleOnChangePart(int partId)
        {
            SearchTerm = "";
            if (partId == 999999)
            {
                var part = new ServiceReportPartModel
                {
                    Id = partId,
                    PartCode = "AdHock",
                    PartDescription = "",
                    SellingPrice = 0,
                    CostPrice = 0,
                    ProductClass = "",
                    Bin = "",
                    CycleCount = 0,
                    QtyOnHand = 0,
                    SupplierCode = "",
                    IsAdHockPart = true
                };

                _serviceReport.Parts.Add(part);
                selectedPart = new ServiceReportPartModel();
            }
            else
            {
                if (_serviceReport.Parts.Any(x => x.Id == partId))
                {
                    return;
                }

                var prt = parts.FirstOrDefault(x => x.Id == partId);
                if (prt == null) return;

                var part = new ServiceReportPartModel
                {
                    Id = prt.Id,
                    PartId = partId,
                    PartCode = prt.PartCode,
                    PartDescription = prt.PartDescription,
                    SellingPrice = Convert.ToDouble(prt.CostPrice),
                    CostPrice = prt.CostPrice,
                    ProductClass = prt.ProductClass,
                    Bin = prt.Bin,
                    CycleCount = prt.CycleCount,
                    QtyOnHand = prt.QtyOnHand,
                    Warehouse = prt.Warehouse,
                    SupplierCode = prt.SupplierCode,
                    IsAdHockPart = false
                };

                _serviceReport.Parts.Add(part);
                selectedPart = new ServiceReportPartModel();
            }
        }

        void HandleOnChangeVehicle(int vehicleId)
        {
            _serviceReport.VehicleId = vehicleId;
            _serviceReport.Vehicle = vehicles.FirstOrDefault(x => x.Id == vehicleId);
        }

        private async Task OnHideModalClick()
        {
            await salesOrderNumberModal.HideAsync();
        }
    }
}
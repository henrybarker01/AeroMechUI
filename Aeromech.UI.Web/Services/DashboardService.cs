using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AeroMech.Models.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
	/// <summary>
	/// Assembles the home screen. Everything here is a read - no figure on the dashboard writes
	/// anything - and none of it has its own opinion about what a report or a quote is worth:
	/// the totals come from <see cref="ServiceReportService.CalculateServiceReportTotal"/> and
	/// <see cref="QuoteService.CalculateQuoteTotal"/> so the dashboard can never disagree with
	/// the grids it links into.
	/// </summary>
	public class DashboardService
	{
		private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
		private readonly ServiceReportService _serviceReportService;
		private readonly QuoteService _quoteService;

		public DashboardService(
			IDbContextFactory<AeroMechDBContext> contextFactory,
			ServiceReportService serviceReportService,
			QuoteService quoteService)
		{
			_contextFactory = contextFactory;
			_serviceReportService = serviceReportService;
			_quoteService = quoteService;
		}

		/// <summary>How many rows the short action lists show before they stop being scannable.</summary>
		private const int ListRows = 6;

		/// <summary>
		/// A quote nobody has answered in this long is not a forecast any more, it is a phone call.
		/// </summary>
		private const int StaleQuoteDays = 30;

		private const int TrendMonths = 6;

		/// <summary>
		/// How far back "recent" reaches. Two weeks is roughly how long a job stays legitimately
		/// open before it is worth asking about.
		/// </summary>
		private const int RecentWorkDays = 14;

		/// <summary>A report with its total worked out once, because five blocks below need it.</summary>
		private sealed record ValuedReport(ServiceReportModel Report, double Total, int AgeDays);

		public async Task<DashboardModel> GetDashboard()
		{
			var today = DateOnly.FromDateTime(DateTime.UtcNow);

			// Both grids already load their whole table this way; the dashboard reads the same
			// sets rather than a second, subtly different query.
			var reports = await _serviceReportService.GetRecentServiceReports();
			var quotes = await _quoteService.GetQuotes();

			var valued = reports
				.Select(r => new ValuedReport(r, _serviceReportService.CalculateServiceReportTotal(r), DaysSince(today, r.ReportDate)))
				.ToList();

			var model = new DashboardModel { StaleQuoteDays = StaleQuoteDays };

			BuildUnbilled(model, valued);
			BuildPipeline(model, quotes, today);
			BuildThisMonth(model, valued, today);
			BuildRecentWork(model, valued);
			BuildTrend(model, valued, today);

			await BuildStockOnHand(model);
			model.Labour = await GetLabourWeek();
			model.MachinesNotSeen = await GetMachinesNotSeen(today);

			return model;
		}

		// ------------------------------------------------------------------
		// What are we owed
		// ------------------------------------------------------------------

		/// <summary>
		/// Work carrying no sales order number.
		///
		/// The sales order is the signal, not <c>IsComplete</c>. The Service Reports grid already
		/// treats "sales order raised" as the last stage a report reaches, and the two fields are
		/// filled in together on the same form - in the live data every single completed report
		/// also carries a sales order, so "complete but unbilled" is empty by construction while
		/// "no sales order" is the pile that actually exists. Age is what separates a report from
		/// last week, which is simply still in progress, from one from last year, which is money
		/// nobody ever claimed.
		/// </summary>
		private static void BuildUnbilled(DashboardModel model, List<ValuedReport> valued)
		{
			var unbilled = valued
				.Where(v => string.IsNullOrWhiteSpace(v.Report.SalesOrderNumber))
				.ToList();

			model.UnbilledTotal = unbilled.Sum(v => v.Total);
			model.UnbilledReportCount = unbilled.Count;
			model.UnbilledOldestDays = unbilled.Count == 0 ? 0 : unbilled.Max(v => v.AgeDays);

			model.UnbilledByClient = unbilled
				.GroupBy(v => new { v.Report.ClientId, Name = v.Report.Client?.Name ?? "No client" })
				.Select(g => new DashboardUnbilledClientModel
				{
					ClientId = g.Key.ClientId,
					ClientName = g.Key.Name,
					Total = g.Sum(v => v.Total),
					ReportCount = g.Count(),
					OldestDays = g.Max(v => v.AgeDays),
					Reports = g
						.OrderByDescending(v => v.AgeDays)
						.Select(v => new DashboardUnbilledReportModel
						{
							Id = v.Report.Id,
							ServiceReportNumber = v.Report.ServiceReportNumber,
							ReportDate = v.Report.ReportDate,
							Machine = DescribeVehicle(v.Report.Vehicle),
							Total = v.Total,
							AgeDays = v.AgeDays
						})
						.ToList()
				})
				.OrderByDescending(x => x.Total)
				.ToList();

			// Fixed bands rather than quartiles: 30/60/90 is how the money is actually argued
			// about, and a band that moves with the data cannot be compared week to week.
			model.UnbilledAgeBuckets = new List<DashboardAgeBucketModel>
			{
				Bucket("0-30 days",  unbilled.Where(v => v.AgeDays <= 30)),
				Bucket("31-60 days", unbilled.Where(v => v.AgeDays > 30 && v.AgeDays <= 60)),
				Bucket("61-90 days", unbilled.Where(v => v.AgeDays > 60 && v.AgeDays <= 90)),
				Bucket("90+ days",   unbilled.Where(v => v.AgeDays > 90))
			};
		}

		private static DashboardAgeBucketModel Bucket(string label, IEnumerable<ValuedReport> reports)
		{
			var list = reports.ToList();
			return new DashboardAgeBucketModel
			{
				Label = label,
				Total = list.Sum(v => v.Total),
				ReportCount = list.Count
			};
		}

		private void BuildPipeline(DashboardModel model, List<QuoteModel> quotes, DateOnly today)
		{
			var open = quotes
				.Where(q => !q.IsConverted)
				.Select(q => new
				{
					Quote = q,
					Total = _quoteService.CalculateQuoteTotal(q),
					AgeDays = DaysSince(today, q.QuoteDate)
				})
				.ToList();

			model.PipelineTotal = open.Sum(x => x.Total);
			model.OpenQuoteCount = open.Count;
			model.StaleQuoteCount = open.Count(x => x.AgeDays > StaleQuoteDays);

			model.OpenQuotes = open
				.OrderByDescending(x => x.AgeDays)
				.Take(ListRows)
				.Select(x => new DashboardOpenQuoteModel
				{
					Id = x.Quote.Id,
					QuoteNumber = x.Quote.QuoteNumber,
					ClientName = x.Quote.Client?.Name ?? "No client",
					Total = x.Total,
					AgeDays = x.AgeDays
				})
				.ToList();
		}

		/// <summary>
		/// Work billed inside the current calendar month, against the same measure last month.
		/// There is no invoice date anywhere in the schema, so this counts by report date: it is
		/// the value of this month's work that reached a sales order, not of money received.
		/// </summary>
		private static void BuildThisMonth(DashboardModel model, List<ValuedReport> valued, DateOnly today)
		{
			var monthStart = new DateOnly(today.Year, today.Month, 1);
			var lastMonthStart = monthStart.AddMonths(-1);

			var thisMonth = valued.Where(v => InMonth(v.Report.ReportDate, monthStart)).ToList();

			model.BilledThisMonth = thisMonth.Where(IsBilled).Sum(v => v.Total);
			model.BilledThisMonthCount = thisMonth.Count(IsBilled);
			model.RaisedThisMonth = thisMonth.Sum(v => v.Total);

			model.BilledLastMonth = valued
				.Where(v => IsBilled(v) && InMonth(v.Report.ReportDate, lastMonthStart))
				.Sum(v => v.Total);
		}

		private static bool IsBilled(ValuedReport report) => !string.IsNullOrWhiteSpace(report.Report.SalesOrderNumber);

		private async Task BuildStockOnHand(DashboardModel model)
		{
			using var context = await _contextFactory.CreateDbContextAsync();

			var parts = await context.Parts
				.AsNoTracking()
				.Include(x => x.Prices)
				.Where(x => !x.IsDeleted)
				.ToListAsync();

			model.StockOnHandValue = parts.Sum(p => p.QtyOnHand * CurrentCostPrice(p));
			model.StockLineCount = parts.Count(p => p.QtyOnHand != 0);
			model.NegativeStockLineCount = parts.Count(p => p.QtyOnHand < 0);
		}

		// ------------------------------------------------------------------
		// What needs doing today
		// ------------------------------------------------------------------

		/// <summary>
		/// Unbilled work from the last fortnight, newest first.
		///
		/// Deliberately separate from the ageing block above, which is about the tail: this is
		/// the near end of the same pile, and it is the half that is simply still in progress.
		/// Reading them together answers whether new work is flowing in and being written up at
		/// the same rate the old work is being cleared.
		/// </summary>
		private static void BuildRecentWork(DashboardModel model, List<ValuedReport> valued)
		{
			var recent = valued
				.Where(v => !IsBilled(v) && v.AgeDays <= RecentWorkDays)
				.ToList();

			model.RecentWorkCount = recent.Count;
			model.RecentWorkDays = RecentWorkDays;

			model.RecentWork = recent
				.OrderBy(v => v.AgeDays)
				.Take(ListRows)
				.Select(v => new DashboardOpenReportModel
				{
					Id = v.Report.Id,
					ServiceReportNumber = v.Report.ServiceReportNumber,
					ClientName = v.Report.Client?.Name ?? "No client",
					Machine = DescribeVehicle(v.Report.Vehicle),
					Total = v.Total,
					AgeDays = v.AgeDays
				})
				.ToList();
		}

		/// <summary>
		/// Hours booked to a service report against hours booked to admin, travel, standby and
		/// leave, for the current week. Both figures already drive the timesheet screens; the
		/// ratio between them is the part nobody currently sees.
		/// </summary>
		private async Task<DashboardLabourWeekModel> GetLabourWeek()
		{
			var monday = MondayOf(DateOnly.FromDateTime(DateTime.Now));
			var nextMonday = monday.AddDays(7);

			using var context = await _contextFactory.CreateDbContextAsync();

			var jobHours = await context.ServiceReportEmployees
				.AsNoTracking()
				.Where(x => x.DutyDate >= monday && x.DutyDate < nextMonday
							&& !x.IsDeleted && !x.Employee!.ExcludeFromTimesheets)
				.GroupBy(x => x.EmployeeId)
				.Select(g => new { EmployeeId = g.Key, Hours = g.Sum(x => x.Hours) })
				.ToDictionaryAsync(x => x.EmployeeId, x => x.Hours);

			var otherHours = await context.TimesheetEmployeeDetails
				.AsNoTracking()
				.Where(x => x.Date >= monday && x.Date < nextMonday
							&& !x.IsDeleted && !x.Employee!.ExcludeFromTimesheets)
				.GroupBy(x => x.EmployeeId)
				.Select(g => new { EmployeeId = g.Key, Hours = g.Sum(x => x.Hours) })
				.ToDictionaryAsync(x => x.EmployeeId, x => x.Hours);

			var employees = await context.Employees
				.AsNoTracking()
				.Where(e => !e.IsDeleted && !e.ExcludeFromTimesheets)
				.ToListAsync();

			var rows = employees
				.Select(e =>
				{
					jobHours.TryGetValue(e.Id, out var job);
					otherHours.TryGetValue(e.Id, out var other);

					return new DashboardLabourEmployeeModel
					{
						EmployeeId = e.Id,
						Name = ShortName(e),
						JobHours = job,
						OtherHours = other
					};
				})
				// Somebody who recorded nothing this week is a timesheet problem, not a
				// utilisation one, and belongs on the timesheet screen rather than here.
				.Where(x => x.TotalHours > 0)
				.OrderByDescending(x => x.JobHours)
				.ToList();

			return new DashboardLabourWeekModel
			{
				WeekStart = monday,
				JobHours = rows.Sum(x => x.JobHours),
				OtherHours = rows.Sum(x => x.OtherHours),
				Employees = rows.Take(ListRows).ToList()
			};
		}

		// ------------------------------------------------------------------
		// Are we growing
		// ------------------------------------------------------------------

		private static void BuildTrend(DashboardModel model, List<ValuedReport> valued, DateOnly today)
		{
			var monthStart = new DateOnly(today.Year, today.Month, 1);

			model.BilledByMonth = Enumerable.Range(0, TrendMonths)
				.Select(i => monthStart.AddMonths(i - (TrendMonths - 1)))
				.Select(start => new DashboardMonthValueModel
				{
					Month = start,
					Label = start.ToString("MMM"),
					IsCurrent = start == monthStart,
					Total = valued
						.Where(v => IsBilled(v) && InMonth(v.Report.ReportDate, start))
						.Sum(v => v.Total)
				})
				.ToList();
		}

		/// <summary>
		/// Machines nobody has touched in the longest, as a list of clients worth phoning.
		/// Measured in days rather than engine hours on purpose: engine hours only ever reach the
		/// system on a service report, so "hours since the last service" would be zero for every
		/// machine by construction.
		/// </summary>
		private async Task<List<DashboardMachineModel>> GetMachinesNotSeen(DateOnly today)
		{
			using var context = await _contextFactory.CreateDbContextAsync();

			var lastSeen = await context.ServiceReports
				.AsNoTracking()
				.Where(r => !r.IsDeleted && r.VehicleId != null)
				.GroupBy(r => r.VehicleId!.Value)
				.Select(g => new { VehicleId = g.Key, LastServiceDate = g.Max(x => x.ReportDate) })
				.OrderBy(x => x.LastServiceDate)
				.Take(ListRows)
				.ToListAsync();

			var vehicleIds = lastSeen.Select(x => x.VehicleId).ToList();

			var vehicles = await context.Vehicles
				.AsNoTracking()
				.Include(v => v.Client)
				.Where(v => !v.IsDeleted && vehicleIds.Contains(v.Id))
				.ToListAsync();

			return lastSeen
				.Join(vehicles, x => x.VehicleId, v => v.Id, (x, v) => new DashboardMachineModel
				{
					VehicleId = v.Id,
					ClientId = v.ClientId,
					Machine = DescribeVehicle(v.MachineType, v.SerialNumber),
					ClientName = v.Client?.Name ?? "No client",
					LastServiceDate = x.LastServiceDate,
					DaysSince = DaysSince(today, x.LastServiceDate)
				})
				.OrderByDescending(x => x.DaysSince)
				.ToList();
		}

		// ------------------------------------------------------------------
		// Helpers
		// ------------------------------------------------------------------

		/// <summary>
		/// Report dates are stored as UTC midnight for the calendar date they were picked on, so
		/// ages are whole days between calendar dates and never a fraction either side of one.
		/// </summary>
		private static int DaysSince(DateOnly today, DateTimeOffset date)
		{
			var days = today.DayNumber - DateOnly.FromDateTime(date.UtcDateTime).DayNumber;
			return days < 0 ? 0 : days;
		}

		private static bool InMonth(DateTimeOffset date, DateOnly monthStart)
		{
			var d = DateOnly.FromDateTime(date.UtcDateTime);
			return d.Year == monthStart.Year && d.Month == monthStart.Month;
		}

		private static DateOnly MondayOf(DateOnly date)
		{
			var dayOfWeek = (int)date.DayOfWeek;
			return date.AddDays(dayOfWeek == 0 ? -6 : -(dayOfWeek - 1));
		}

		/// <summary>
		/// What the dashboard values stock at. Kept in step with
		/// <c>StockReportService.CurrentCostPrice</c> and <c>StockTakeService.CurrentCostPrice</c>
		/// deliberately: three screens disagreeing about what a part costs would be worse than any
		/// one of them being wrong on its own.
		/// </summary>
		private static double CurrentCostPrice(Part part)
			=> part.Prices?.OrderBy(x => x.Id).FirstOrDefault()?.CostPrice ?? 0;

		private static string DescribeVehicle(VehicleModel? vehicle)
			=> vehicle is null ? "" : DescribeVehicle(vehicle.MachineType, vehicle.SerialNumber);

		private static string DescribeVehicle(string? machineType, string? serialNumber)
		{
			var parts = new[] { machineType, serialNumber }
				.Where(x => !string.IsNullOrWhiteSpace(x))
				.Select(x => x!.Trim());

			return string.Join(" / ", parts);
		}

		private static string ShortName(Employee employee)
		{
			var first = (employee.FirstName ?? "").Trim();
			var last = (employee.LastName ?? "").Trim();

			// Six names in a narrow column: an initial and a surname fits where a full name wraps.
			if (first.Length > 0 && last.Length > 0) return $"{first[0]}. {last}";

			return first.Length > 0 ? first : last;
		}
	}
}

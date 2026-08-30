namespace AeroMech.Models.Models
{
	/// <summary>
	/// Everything the home screen shows, read in one pass. The screen answers four questions in
	/// order - what are we owed, who owes it, what needs doing today, are we growing - and the
	/// shape of this model follows that order rather than following the tables underneath.
	/// </summary>
	public class DashboardModel
	{
		// --- what are we owed -------------------------------------------------

		/// <summary>
		/// Value of work carrying no sales order number. This is the headline figure: the job is
		/// written up, the stock is gone, and nobody has raised an invoice for it. The sales order
		/// is the test rather than <c>IsComplete</c>, because the two are filled in together on the
		/// same form and the sales order is the one the Service Reports grid already reads.
		/// </summary>
		public double UnbilledTotal { get; set; }
		public int UnbilledReportCount { get; set; }

		/// <summary>Age of the oldest unbilled report, in days. Zero when there are none.</summary>
		public int UnbilledOldestDays { get; set; }

		public double PipelineTotal { get; set; }
		public int OpenQuoteCount { get; set; }

		/// <summary>Open quotes older than <see cref="StaleQuoteDays"/>, which are the ones to chase.</summary>
		public int StaleQuoteCount { get; set; }
		public int StaleQuoteDays { get; set; }

		/// <summary>Value of this month's work that reached a sales order.</summary>
		public double BilledThisMonth { get; set; }
		public int BilledThisMonthCount { get; set; }
		public double BilledLastMonth { get; set; }

		/// <summary>
		/// Everything written up this month, billed or not. The gap between this and
		/// <see cref="BilledThisMonth"/> is this month's own contribution to the unbilled pile.
		/// </summary>
		public double RaisedThisMonth { get; set; }

		public double StockOnHandValue { get; set; }
		public int StockLineCount { get; set; }

		/// <summary>Parts sitting at a negative quantity, which means the ledger has drifted from the shelf.</summary>
		public int NegativeStockLineCount { get; set; }

		// --- who owes it ------------------------------------------------------

		public List<DashboardUnbilledClientModel> UnbilledByClient { get; set; } = new();
		public List<DashboardAgeBucketModel> UnbilledAgeBuckets { get; set; } = new();

		// --- what needs doing today -------------------------------------------

		public List<DashboardOpenQuoteModel> OpenQuotes { get; set; } = new();

		/// <summary>
		/// Unbilled reports from the last <see cref="RecentWorkDays"/> days - the near end of the
		/// same pile the ageing block reads from the far end.
		/// </summary>
		public List<DashboardOpenReportModel> RecentWork { get; set; } = new();
		public int RecentWorkCount { get; set; }
		public int RecentWorkDays { get; set; }

		public DashboardLabourWeekModel Labour { get; set; } = new();

		// --- are we growing ---------------------------------------------------

		public List<DashboardMonthValueModel> BilledByMonth { get; set; } = new();
		public List<DashboardMachineModel> MachinesNotSeen { get; set; } = new();
	}

	/// <summary>
	/// One client's unbilled exposure, with the individual reports behind it so the screen can
	/// open a client in place rather than sending the reader to a filtered grid.
	/// </summary>
	public class DashboardUnbilledClientModel
	{
		public int ClientId { get; set; }
		public string ClientName { get; set; } = "";
		public double Total { get; set; }
		public int ReportCount { get; set; }
		public int OldestDays { get; set; }
		public List<DashboardUnbilledReportModel> Reports { get; set; } = new();
	}

	public class DashboardUnbilledReportModel
	{
		public int Id { get; set; }
		public int ServiceReportNumber { get; set; }
		public DateTimeOffset ReportDate { get; set; }

		/// <summary>Machine type and serial, already joined for display. Empty when no vehicle was recorded.</summary>
		public string Machine { get; set; } = "";
		public double Total { get; set; }
		public int AgeDays { get; set; }
	}

	public class DashboardAgeBucketModel
	{
		public string Label { get; set; } = "";
		public double Total { get; set; }
		public int ReportCount { get; set; }
	}

	public class DashboardOpenQuoteModel
	{
		public int Id { get; set; }
		public int QuoteNumber { get; set; }
		public string ClientName { get; set; } = "";
		public double Total { get; set; }
		public int AgeDays { get; set; }
	}

	public class DashboardOpenReportModel
	{
		public int Id { get; set; }
		public int ServiceReportNumber { get; set; }
		public string ClientName { get; set; } = "";
		public string Machine { get; set; } = "";
		public double Total { get; set; }
		public int AgeDays { get; set; }
	}

	/// <summary>
	/// The week's hours split into work that can be invoiced and work that cannot. Both halves
	/// already exist in the timesheet screens; the ratio between them is the part nobody sees.
	/// </summary>
	public class DashboardLabourWeekModel
	{
		public DateOnly WeekStart { get; set; }
		public double JobHours { get; set; }
		public double OtherHours { get; set; }
		public double TotalHours => JobHours + OtherHours;

		/// <summary>Share of recorded hours booked to a service report, 0-100. Zero when nothing is recorded.</summary>
		public int BillablePercent => TotalHours <= 0 ? 0 : (int)Math.Round(JobHours / TotalHours * 100);

		public List<DashboardLabourEmployeeModel> Employees { get; set; } = new();
	}

	public class DashboardLabourEmployeeModel
	{
		public int EmployeeId { get; set; }
		public string Name { get; set; } = "";
		public double JobHours { get; set; }
		public double OtherHours { get; set; }
		public double TotalHours => JobHours + OtherHours;
		public int BillablePercent => TotalHours <= 0 ? 0 : (int)Math.Round(JobHours / TotalHours * 100);
	}

	public class DashboardMonthValueModel
	{
		public DateOnly Month { get; set; }
		public string Label { get; set; } = "";
		public double Total { get; set; }

		/// <summary>The month still running. Drawn at full strength and labelled; the rest recede.</summary>
		public bool IsCurrent { get; set; }
	}

	/// <summary>
	/// A machine and how long since anyone touched it. Deliberately measured in days rather than
	/// engine hours: engine hours only ever reach the system on a service report, so hours since
	/// the last service would be zero for every machine by construction.
	/// </summary>
	public class DashboardMachineModel
	{
		public int VehicleId { get; set; }
		public int? ClientId { get; set; }
		public string Machine { get; set; } = "";
		public string ClientName { get; set; } = "";
		public DateTimeOffset LastServiceDate { get; set; }
		public int DaysSince { get; set; }
	}
}

using System.Globalization;
using AeroMech.Models.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace AeroMech.UI.Web.Pages.Dashboard
{
	public partial class DashboardView
	{
		[Inject] private NavigationManager NavigationManager { get; set; } = default!;

		[Parameter, EditorRequired] public DashboardModel Model { get; set; } = default!;

		/// <summary>
		/// Clients the reader has opened, held by id rather than by row so the list can be
		/// re-sorted or reloaded underneath without a different client springing open.
		/// </summary>
		private readonly HashSet<int> _openClients = new();

		// ------------------------------------------------------------------
		// Drill down
		// ------------------------------------------------------------------

		private void ToggleClient(int clientId)
		{
			if (!_openClients.Add(clientId))
				_openClients.Remove(clientId);
		}

		private void OnClientKey(KeyboardEventArgs args, int clientId)
		{
			if (args.Key is "Enter" or " ") ToggleClient(clientId);
		}

		private void OnReportKey(KeyboardEventArgs args, int reportId)
		{
			if (args.Key is "Enter" or " ") OpenReport(reportId);
		}

		private void OpenReport(int reportId) => NavigationManager.NavigateTo($"/add-service-report/{reportId}");

		private void OpenQuote(int quoteId) => NavigationManager.NavigateTo($"/add-quote/{quoteId}");

		// ------------------------------------------------------------------
		// Scales
		//
		// Every bar is drawn against the largest value in its own block, so the longest bar
		// always fills its track and the differences between the rest stay readable.
		// ------------------------------------------------------------------

		private double MaxClientTotal =>
			Model.UnbilledByClient.Count == 0 ? 0 : Model.UnbilledByClient.Max(x => x.Total);

		private double MaxBucketTotal =>
			Model.UnbilledAgeBuckets.Count == 0 ? 0 : Model.UnbilledAgeBuckets.Max(x => x.Total);

		private double MaxMonthTotal =>
			Model.BilledByMonth.Count == 0 ? 0 : Model.BilledByMonth.Max(x => x.Total);

		private DashboardAgeBucketModel? OldestBucket => Model.UnbilledAgeBuckets.LastOrDefault();

		// ------------------------------------------------------------------
		// Formatting
		// ------------------------------------------------------------------

		/// <summary>
		/// Whole rands. Cents on a dashboard are noise, and the currency symbol is the one the
		/// culture middleware sets, so this matches the grids.
		/// </summary>
		private static string Money(double value) => value.ToString("C0", CultureInfo.CurrentCulture);

		private static string Hours(double value) => $"{value.ToString("0.#", CultureInfo.CurrentCulture)}h";

		/// <summary>
		/// A width for a style attribute, so it is written with a decimal point whatever the
		/// current culture would otherwise use. A comma here silently breaks the declaration.
		/// </summary>
		private static string Pct(double part, double whole)
			=> whole <= 0 ? "0" : (part / whole * 100).ToString("0.##", CultureInfo.InvariantCulture);

		private static string Plural(int count, string word) => count == 1 ? word : word + "s";

		private static string Describe(DashboardOpenReportModel report)
			=> string.IsNullOrWhiteSpace(report.Machine)
				? report.ClientName
				: $"{report.ClientName} · {report.Machine}";

		// ------------------------------------------------------------------
		// Status
		//
		// The age ramp is one blue stepped light to dark, so older reads as heavier without
		// asking colour alone to carry it - every bar is labelled with its band and its value.
		// The pills are the app's own status colours and always sit beside the number.
		// ------------------------------------------------------------------

		private string AgeFill(DashboardAgeBucketModel bucket)
			=> $"fill-age-{Model.UnbilledAgeBuckets.IndexOf(bucket) + 1}";

		private static string AgePill(int days) => days > 90 ? "pill-bad" : days > 60 ? "pill-warn" : "pill-ok";

		private string QuotePill(int days)
			=> days > Model.StaleQuoteDays * 1.5 ? "pill-bad"
				: days > Model.StaleQuoteDays ? "pill-warn"
				: "pill-ok";

		private static string MachinePill(int days) => days > 180 ? "pill-bad" : days > 90 ? "pill-warn" : "pill-ok";
	}
}

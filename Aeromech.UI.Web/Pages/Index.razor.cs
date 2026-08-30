using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace AeroMech.UI.Web.Pages
{
	public partial class Index
	{
		[Inject] private DashboardService DashboardService { get; set; } = default!;
		[Inject] private LoaderService LoaderService { get; set; } = default!;

		/// <summary>
		/// This route is also what an anonymous visitor lands on - the sign in form is the
		/// NotAuthorized branch of this same page - so the dashboard query has to wait for a
		/// signed in user. Without it the login screen reads the whole workshop behind its own
		/// form and shows the loader over it.
		/// </summary>
		[CascadingParameter] private Task<AuthenticationState>? AuthenticationStateTask { get; set; }

		private DashboardModel? _model;

		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			if (!firstRender || AuthenticationStateTask is null) return;

			var authenticationState = await AuthenticationStateTask;
			if (authenticationState.User?.Identity?.IsAuthenticated != true) return;

			LoaderService.ShowLoader();
			try
			{
				_model = await DashboardService.GetDashboard();
			}
			finally
			{
				LoaderService.HideLoader();
			}

			await InvokeAsync(StateHasChanged);
		}
	}
}

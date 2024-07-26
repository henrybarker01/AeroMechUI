using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AeroMech.UI.Web.Pages.Login
{
	public partial class Login
	{
		[Inject] UserService _userService { get; set; }

		public SignInManager<IdentityUser>? SignInManager { get; init; }


		private Credential _credential = new Credential();

		private async void Authenticate()
		{
			await _userService.LoginAsync(_credential);
		}

	}
}
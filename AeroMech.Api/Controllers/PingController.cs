using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AeroMech.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PingController : Controller
	{
		private readonly IConfiguration _configuration;
        public PingController(IConfiguration configuration)

		{
            _configuration = configuration;
        }

        public IActionResult Index()
		{
			return new OkObjectResult("Hello Areo Mech application. V2");
		}

		[Route("get-settings")]
		[HttpGet(Name = "Get Settingst")]
		public IActionResult GetSettings()
		{
			var test = _configuration.GetConnectionString("DefaultConnection");
			return new OkObjectResult(test);
		}
	}
}

using Newtonsoft.Json.Linq;
using System.Globalization;

namespace AeroMech.UI.Web.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string token;
            if (context.Request.Cookies.TryGetValue("token", out token))
            {
              //  return token;
            }
           // return token;

            // Call the next delegate/middleware in the pipeline.
            await _next(context);
        }
    }
}   
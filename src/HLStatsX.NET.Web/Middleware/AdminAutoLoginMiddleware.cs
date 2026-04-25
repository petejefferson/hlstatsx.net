using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HLStatsX.NET.Web.Middleware;

public class AdminAutoLoginMiddleware
{
    private readonly RequestDelegate _next;

    public AdminAutoLoginMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/Admin") &&
            context.User.Identity?.IsAuthenticated != true)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "pete"),
                new(ClaimTypes.Role, "Admin"),
                new("AccLevel", "100")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            context.User = principal;
        }

        await _next(context);
    }
}

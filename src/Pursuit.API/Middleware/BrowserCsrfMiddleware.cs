using Microsoft.AspNetCore.Antiforgery;

namespace Pursuit.API.Middleware;

public sealed class BrowserCsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;

    public BrowserCsrfMiddleware(RequestDelegate next, string[] allowedOrigins)
    {
        _next = next;
        _allowedOrigins = new HashSet<string>(allowedOrigins, StringComparer.OrdinalIgnoreCase);
    }

    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var request = context.Request;
        if (!request.Path.StartsWithSegments("/api")
            || HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method) || HttpMethods.IsTrace(request.Method))
        {
            await _next(context);
            return;
        }

        var origin = request.Headers.Origin.ToString();
        if ((request.Headers.ContainsKey("Origin")
                && !IsTrustedOrigin(origin, request))
            || (!request.Headers.ContainsKey("Origin")
                && string.Equals(request.Headers["Sec-Fetch-Site"], "cross-site", StringComparison.OrdinalIgnoreCase)))
        {
            await DenyAsync(context, StatusCodes.Status403Forbidden, "Untrusted request origin.");
            return;
        }

        var hasAuthCookie = request.Cookies.ContainsKey("pursuit_token")
            || request.Cookies.ContainsKey("pursuit_refresh_token");
        var bearerOnly = !hasAuthCookie
            && !request.Path.StartsWithSegments("/api/auth")
            && request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            && context.User.Identity?.IsAuthenticated == true;

        if (!bearerOnly && !await antiforgery.IsRequestValidAsync(context))
        {
            await DenyAsync(context, StatusCodes.Status400BadRequest, "CSRF validation failed.");
            return;
        }

        await _next(context);
    }

    private bool IsTrustedOrigin(string origin, HttpRequest request)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || uri.UserInfo.Length != 0
            || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            return false;

        var normalized = uri.GetLeftPart(UriPartial.Authority);
        return _allowedOrigins.Contains(normalized)
            || (string.Equals(uri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task DenyAsync(HttpContext context, int status, string message)
    {
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message });
    }
}

using Microsoft.AspNetCore.Mvc;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Pursuit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private const string AccessTokenCookieName = "pursuit_token";
    private const string RefreshTokenCookieName = "pursuit_refresh_token";
    private const string AccessTokenCookiePath = "/";
    private const string RefreshTokenCookiePath = "/api/auth/refresh";
    private const string RefreshLogoutPath = "/api/auth/refresh/logout";

    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(dto, cancellationToken);
        SetTokenCookie(result.Token);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new { result.Email, result.Role });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        SetTokenCookie(result.Token);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(new { result.Email, result.Role });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken)
            || string.IsNullOrEmpty(refreshToken))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var result = await _authService.RefreshTokenAsync(refreshToken, cancellationToken);
        SetTokenCookie(result.Token);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var tenantId = User.FindFirst("tenantId")?.Value;

        return Ok(new { email, role, tenantId });
    }

    [HttpPost("logout")]
    public IActionResult LegacyLogout()
    {
        return RedirectPreserveMethod(RefreshLogoutPath);
    }

    [HttpPost("refresh/logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken)
            && !string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken, cancellationToken);
        }

        Response.Cookies.Delete(AccessTokenCookieName, CreateCookieOptions(AccessTokenCookiePath));
        Response.Cookies.Delete(RefreshTokenCookieName, CreateCookieOptions(RefreshTokenCookiePath));

        return Ok();
    }

    private void SetTokenCookie(string token)
    {
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!);

        Response.Cookies.Append(
            AccessTokenCookieName,
            token,
            CreateCookieOptions(AccessTokenCookiePath, DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)));
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var expiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"]!);

        Response.Cookies.Append(
            RefreshTokenCookieName,
            refreshToken,
            CreateCookieOptions(RefreshTokenCookiePath, DateTimeOffset.UtcNow.AddDays(expiryDays)));
    }

    private CookieOptions CreateCookieOptions(string path, DateTimeOffset? expires = null)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = path,
            Expires = expires
        };
    }
}

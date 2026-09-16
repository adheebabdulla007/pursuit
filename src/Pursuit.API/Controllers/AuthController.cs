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
        if (!Request.Cookies.TryGetValue("pursuit_refresh_token", out var refreshToken)
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
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue("pursuit_refresh_token", out var refreshToken)
            && !string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken, cancellationToken);
        }

        Response.Cookies.Delete("pursuit_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        Response.Cookies.Delete("pursuit_refresh_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth/refresh"
        });

        return Ok();
    }

    private void SetTokenCookie(string token)
    {
        var expiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"]!);

        Response.Cookies.Append("pursuit_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
        });
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var expiryDays = int.Parse(_configuration["JwtSettings:RefreshTokenExpiryInDays"]!);

        Response.Cookies.Append("pursuit_refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth/refresh",
            Expires = DateTimeOffset.UtcNow.AddDays(expiryDays)
        });
    }
}
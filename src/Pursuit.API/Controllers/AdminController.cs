using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pursuit.Application.Interfaces;

namespace Pursuit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IUserService _userService;

    public AdminController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetAllUsersAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}
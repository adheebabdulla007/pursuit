using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;

namespace Pursuit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    [HttpPost]
    [Authorize(Roles = "JobSeeker")]
    public async Task<IActionResult> Apply(
        [FromBody] CreateApplicationDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.ApplyAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetMyApplications), null, result);
    }

    [HttpGet("my")]
    [Authorize(Roles = "JobSeeker")]
    public async Task<IActionResult> GetMyApplications(
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetMyApplicationsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("job/{jobId:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetByJob(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetByJobAsync(jobId, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateApplicationStatusDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.UpdateStatusAsync(id, dto, cancellationToken);
        return Ok(result);
    }
}
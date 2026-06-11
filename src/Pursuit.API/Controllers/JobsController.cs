using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;
using Pursuit.Domain.Enums;

namespace Pursuit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? keyword,
        [FromQuery] string? location,
        [FromQuery] JobType? jobType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _jobService.SearchAsync(
            keyword, location, jobType, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _jobService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Create(
        [FromBody] CreateJobDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _jobService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateJobDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _jobService.UpdateAsync(id, dto, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _jobService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
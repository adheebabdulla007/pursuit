using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pursuit.API.Middleware;
using Pursuit.Application.Common;
using Pursuit.Application.DTOs;
using Pursuit.Application.Interfaces;

namespace Pursuit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;
    private readonly IBlobStorageService _blobStorageService;

    private static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public ApplicationsController(
        IApplicationService applicationService,
        IBlobStorageService blobStorageService)
    {
        _applicationService = applicationService;
        _blobStorageService = blobStorageService;
    }

    [HttpPost]
    [Authorize(Roles = "JobSeeker")]
    public async Task<IActionResult> Apply(
        [FromForm] Guid jobId,
        IFormFile resume,
        CancellationToken cancellationToken)
    {
        if (jobId == Guid.Empty)
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "JobId is required." }
);

        if (resume is null || resume.Length == 0)
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Resume file is required." });

        if (!AllowedContentTypes.Contains(resume.ContentType))
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "Only PDF and DOCX files are allowed." });

        if (resume.Length > 5 * 1024 * 1024)
            return BadRequest(new ErrorResponse { StatusCode = 400, Message = "File size must not exceed 5MB." });

        await _applicationService.EnsureCanApplyAsync(jobId, cancellationToken);

        await using var stream = resume.OpenReadStream();
        var resumeUrl = await _blobStorageService.UploadAsync(
            stream, resume.FileName, resume.ContentType, cancellationToken);

        var dto = new CreateApplicationDto
        {
            JobId = jobId,
            ResumeUrl = resumeUrl
        };

        var result = await _applicationService.ApplyAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetMyApplications), null, result);
    }

    [HttpGet("my")]
    [Authorize(Roles = "JobSeeker")]
    public async Task<IActionResult> GetMyApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationPolicy.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.GetMyApplicationsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("job/{jobId:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetByJob(
        Guid jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationPolicy.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.GetByJobAsync(jobId, page, pageSize, cancellationToken);
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

    [HttpGet("{id:guid}/resume")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetResumeDownloadUrl(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resumeStorageUrl = await _applicationService.GetResumeStorageUrlAsync(id, cancellationToken);

        if (string.IsNullOrEmpty(resumeStorageUrl))
            return NotFound(value: new ErrorResponse { StatusCode = 404, Message = "No resume found for this application." });

        var sasUrl = await _blobStorageService.GetDownloadUrlAsync(
            resumeStorageUrl, TimeSpan.FromMinutes(15), cancellationToken);

        return Ok(new { downloadUrl = sasUrl });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }
}

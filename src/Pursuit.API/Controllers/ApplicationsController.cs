using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pursuit.API.Middleware;
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

    [HttpGet("{id:guid}/resume")]
    [Authorize(Roles = "Employer")]
    public async Task<IActionResult> GetResumeDownloadUrl(
        Guid id,
        CancellationToken cancellationToken)
    {
        var application = await _applicationService.GetByIdAsync(id, cancellationToken);

        if (string.IsNullOrEmpty(application.ResumeUrl))
            return NotFound(value: new ErrorResponse { StatusCode = 404, Message = "No resume found for this application." });

        var sasUrl = await _blobStorageService.GetDownloadUrlAsync(
            application.ResumeUrl, TimeSpan.FromMinutes(15), cancellationToken);

        return Ok(new { downloadUrl = sasUrl });
    }
}
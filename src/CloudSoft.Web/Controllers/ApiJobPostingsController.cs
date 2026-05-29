using Microsoft.AspNetCore.Mvc;
using CloudSoft.Domain;
using CloudSoft.Services;
using CloudSoft.Web.Dtos;

namespace CloudSoft.Web.Controllers;

/// <summary>
/// REST API controller for job postings. Accessible via API key authentication.
/// Runs alongside the MVC controllers and uses the same service layer.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ApiJobPostingsController : ControllerBase
{
    private readonly IJobPostingService _jobPostingService;
    private readonly ILogger<ApiJobPostingsController> _logger;

    public ApiJobPostingsController(
        IJobPostingService jobPostingService,
        ILogger<ApiJobPostingsController> logger)
    {
        _jobPostingService = jobPostingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobPostingOutputDto>>> GetAll()
    {
        var postings = await _jobPostingService.GetAllAsync(HttpContext.RequestAborted);
        var result = postings.Select(MapToOutputDto);
        _logger.LogInformation("API: Retrieved {Count} job postings.", result.Count());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JobPostingOutputDto>> GetById(string id)
    {
        var posting = await _jobPostingService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (posting == null)
        {
            return NotFound();
        }

        _logger.LogInformation("API: Retrieved job posting '{Id}'.", id);
        return Ok(MapToOutputDto(posting));
    }

    [HttpPost]
    public async Task<ActionResult<JobPostingOutputDto>> Create(JobPostingDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var posting = new JobPosting
        {
            Title = dto.Title,
            Location = dto.Location,
            Description = dto.Description,
            Status = Enum.Parse<JobPostingStatus>(dto.Status, ignoreCase: true),
        };

        if (!posting.IsValid(out string? error))
        {
            return BadRequest(new { error });
        }

        await _jobPostingService.CreateAsync(posting, HttpContext.RequestAborted);
        _logger.LogInformation("API: Created job posting '{Title}' with Id '{Id}'.", posting.Title, posting.Id);

        return CreatedAtAction(
            nameof(GetById),
            new { id = posting.Id },
            MapToOutputDto(posting));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<JobPostingOutputDto>> Update(string id, JobPostingDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = await _jobPostingService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (existing == null)
        {
            return NotFound();
        }

        var posting = new JobPosting
        {
            Id = id,
            Title = dto.Title,
            Location = dto.Location,
            Description = dto.Description,
            Status = Enum.Parse<JobPostingStatus>(dto.Status, ignoreCase: true),
            IsActive = existing.IsActive,
            CreatedAt = existing.CreatedAt,
        };

        if (!posting.IsValid(out string? error))
        {
            return BadRequest(new { error });
        }

        await _jobPostingService.UpdateAsync(posting, HttpContext.RequestAborted);
        _logger.LogInformation("API: Updated job posting '{Id}'.", id);

        return Ok(MapToOutputDto(posting));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existing = await _jobPostingService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (existing == null)
        {
            return NotFound();
        }

        await _jobPostingService.DeleteAsync(id, HttpContext.RequestAborted);
        _logger.LogInformation("API: Deleted job posting '{Id}'.", id);

        return NoContent();
    }

    private static JobPostingOutputDto MapToOutputDto(JobPosting posting)
    {
        return new JobPostingOutputDto
        {
            Id = posting.Id,
            Title = posting.Title,
            Location = posting.Location,
            Description = posting.Description,
            Status = posting.Status.ToString(),
            IsActive = posting.IsActive,
            CreatedAt = posting.CreatedAt,
            UpdatedAt = posting.UpdatedAt,
        };
    }
}

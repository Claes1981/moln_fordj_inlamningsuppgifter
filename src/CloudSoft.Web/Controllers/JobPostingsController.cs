using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CloudSoft.Domain;
using CloudSoft.Services;

namespace CloudSoft.Web.Controllers;

[Authorize(Roles = Constants.AdministratorRole)]
public class JobPostingsController : Controller
{
    private readonly IJobPostingService _jobPostingService;
    private readonly ILogger<JobPostingsController> _logger;

    public JobPostingsController(
        IJobPostingService jobPostingService,
        ILogger<JobPostingsController> logger)
    {
        _jobPostingService = jobPostingService;
        _logger = logger;
    }

    private static SelectList StatusSelectList(JobPostingStatus selected = JobPostingStatus.Draft)
    {
        return new SelectList(Enum.GetValues<JobPostingStatus>(), "ToString()", "ToString()", selected);
    }

    public async Task<IActionResult> Index()
    {
        var jobPostings = await _jobPostingService.GetAllAsync(HttpContext.RequestAborted);
        return View(jobPostings);
    }

    public IActionResult Create()
    {
        ViewData["Status"] = StatusSelectList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobPosting jobPosting)
    {
        if (!jobPosting.IsValid(out string? error))
        {
            ViewData["Status"] = StatusSelectList(jobPosting.Status);
            ModelState.AddModelError("", error!);
            return View(jobPosting);
        }

        try
        {
            await _jobPostingService.CreateAsync(jobPosting, HttpContext.RequestAborted);
            _logger.LogInformation("Job posting '{Title}' created with Id '{Id}'.", jobPosting.Title, jobPosting.Id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create job posting '{Title}': {Message}", jobPosting.Title, ex.Message);
            ViewData["Status"] = StatusSelectList(jobPosting.Status);
            ModelState.AddModelError("", ex.Message);
            return View(jobPosting);
        }
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var jobPosting = await _jobPostingService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (jobPosting == null)
        {
            return NotFound();
        }

        return View(jobPosting);
    }

    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var jobPosting = await _jobPostingService.GetByIdAsync(id, HttpContext.RequestAborted);
        if (jobPosting == null)
        {
            return NotFound();
        }

        ViewData["Status"] = StatusSelectList(jobPosting.Status);
        return View(jobPosting);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, JobPosting jobPosting)
    {
        if (id != jobPosting.Id)
        {
            return NotFound();
        }

        if (!jobPosting.IsValid(out string? error))
        {
            ViewData["Status"] = StatusSelectList(jobPosting.Status);
            ModelState.AddModelError("", error!);
            return View(jobPosting);
        }

        try
        {
            await _jobPostingService.UpdateAsync(jobPosting, HttpContext.RequestAborted);
            _logger.LogInformation("Job posting '{Title}' updated with Id '{Id}'.", jobPosting.Title, jobPosting.Id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update job posting '{Id}': {Message}", id, ex.Message);
            ViewData["Status"] = StatusSelectList(jobPosting.Status);
            ModelState.AddModelError("", ex.Message);
            return View(jobPosting);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            var posting = await _jobPostingService.GetByIdAsync(id, HttpContext.RequestAborted);
            await _jobPostingService.DeleteAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Job posting '{Title}' deleted with Id '{Id}'.", posting?.Title ?? "Unknown", id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to delete job posting '{Id}': {Message}", id, ex.Message);
            ModelState.AddModelError("", ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(string id)
    {
        try
        {
            await _jobPostingService.PublishAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Job posting published with Id '{Id}'.", id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to publish job posting '{Id}': {Message}", id, ex.Message);
            ModelState.AddModelError("", ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(string id)
    {
        try
        {
            await _jobPostingService.CloseAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Job posting closed with Id '{Id}'.", id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to close job posting '{Id}': {Message}", id, ex.Message);
            ModelState.AddModelError("", ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

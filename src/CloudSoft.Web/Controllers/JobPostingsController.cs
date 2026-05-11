using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CloudSoft.Domain;
using CloudSoft.Services;

namespace CloudSoft.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class JobPostingsController : Controller
{
    private readonly IJobPostingService _jobPostingService;

    public JobPostingsController(IJobPostingService jobPostingService)
    {
        _jobPostingService = jobPostingService;
    }

    public async Task<IActionResult> Index()
    {
        var jobPostings = await _jobPostingService.GetAllAsync();
        return View(jobPostings);
    }

    public IActionResult Create()
    {
        ViewData["Status"] = new SelectList(Enum.GetValues<JobPostingStatus>(), "ToString()", "ToString()", JobPostingStatus.Draft);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JobPosting jobPosting)
    {
        if (!jobPosting.IsValid(out _))
        {
            ViewData["Status"] = new SelectList(Enum.GetValues<JobPostingStatus>(), "ToString()", "ToString()", jobPosting.Status);
            foreach (var key in ModelState.Keys.ToList())
            {
                var val = ModelState[key];
                if (val != null && val.Errors.Count > 0)
                {
                    val.Errors.Clear();
                }
            }
            ModelState.AddModelError("", "Invalid job posting data.");
            return View(jobPosting);
        }

        try
        {
            await _jobPostingService.CreateAsync(jobPosting);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ViewData["Status"] = new SelectList(Enum.GetValues<JobPostingStatus>(), "ToString()", "ToString()", jobPosting.Status);
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

        var jobPosting = await _jobPostingService.GetByIdAsync(id);
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

        var jobPosting = await _jobPostingService.GetByIdAsync(id);
        if (jobPosting == null)
        {
            return NotFound();
        }

        ViewData["Status"] = new SelectList(Enum.GetValues<JobPostingStatus>(), "ToString()", "ToString()", jobPosting.Status);
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

        if (!jobPosting.IsValid(out _))
        {
            ViewData["Status"] = new SelectList(Enum.GetValues<JobPostingStatus>(), "ToString()", "ToString()", jobPosting.Status);
            ModelState.AddModelError("", "Invalid job posting data.");
            return View(jobPosting);
        }

        try
        {
            await _jobPostingService.UpdateAsync(jobPosting);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ViewData["Status"] = new SelectList(Enum.GetValues<JobPostingStatus>(), "ToString()", "ToString()", jobPosting.Status);
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
            await _jobPostingService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
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
            await _jobPostingService.PublishAsync(id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
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
            await _jobPostingService.CloseAsync(id);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

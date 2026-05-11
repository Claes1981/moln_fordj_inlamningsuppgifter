using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CloudSoft.Services;
using CloudSoft.Web.Models;

namespace CloudSoft.Web.Controllers;

public class HomeController : Controller
{
    private readonly IJobPostingService _jobPostingService;

    public HomeController(IJobPostingService jobPostingService)
    {
        _jobPostingService = jobPostingService;
    }

    public async Task<IActionResult> Index()
    {
        var jobPostings = await _jobPostingService.GetPublishedAsync();
        return View(jobPostings);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

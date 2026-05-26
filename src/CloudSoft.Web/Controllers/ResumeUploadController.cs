using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CloudSoft.Domain;
using CloudSoft.Web.Utilities;

namespace CloudSoft.Web.Controllers;

/// <summary>
/// Handles resume PDF uploads via the MVC form.
/// Validates file type using magic bytes and uploads to Azure Blob Storage.
/// </summary>
[Authorize(Roles = Constants.CandidateRole + "," + Constants.AdministratorRole)]
public class ResumeUploadController : Controller
{
    private readonly IBlobService _blobService;
    private readonly ILogger<ResumeUploadController> _logger;

    public ResumeUploadController(
        IBlobService blobService,
        ILogger<ResumeUploadController> logger)
    {
        _blobService = blobService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("file", "No file uploaded.");
            return View("Index");
        }

        // Validate PDF by magic bytes (not just extension)
        using var stream = file.OpenReadStream();
        if (!PdfValidation.IsPdf(stream))
        {
            _logger.LogWarning("Non-PDF file upload attempt: '{FileName}'.", file.FileName);
            ModelState.AddModelError("file", "Uploaded file is not a valid PDF.");
            return View("Index");
        }

        // Enforce max file size (5 MB)
        if (file.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError("file", "File must be smaller than 5 MB.");
            return View("Index");
        }

        var blobName = $"{User.Identity?.Name ?? "anonymous"}/{Guid.NewGuid():N}_{file.FileName}";
        try
        {
            stream.Position = 0;
            var url = await _blobService.UploadAsync("resumes", blobName, stream, HttpContext.RequestAborted);
            _logger.LogInformation("Resume uploaded: '{BlobName}' by user '{User}'.", blobName, User.Identity?.Name);

            ViewData["UploadUrl"] = url;
            ViewData["Success"] = true;
            return View("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload resume for user '{User}'.", User.Identity?.Name);
            ModelState.AddModelError("file", "Upload failed. Please try again.");
            return View("Index");
        }
    }
}

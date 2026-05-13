using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Web.Controllers;

public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}

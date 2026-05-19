using Microsoft.AspNetCore.Mvc;
using CloudSoft.Web.Models;

namespace CloudSoft.Web.Controllers;

public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new HealthResponse { Timestamp = DateTime.UtcNow });
    }
}

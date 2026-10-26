// Midlertidigt test-endpoint for authentication. 
// Skal fjernes når vi har et rigtigt endpoint for det.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameBackend.Api.Controllers;

[ApiController]
[Route("api/ping")]
[Authorize]
public class PingController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        sub = User.FindFirst("sub")?.Value,
        name = User.Identity?.Name,
        role = User.FindFirst("role")?.Value
    });
}
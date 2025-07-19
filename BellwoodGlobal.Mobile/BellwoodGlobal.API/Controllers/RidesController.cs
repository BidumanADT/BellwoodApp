// File: Controllers/RidesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BellwoodGlobal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RidesController : ControllerBase
    {
        [HttpGet]
        [Authorize(Policy = "RequireRideScope")]
        public IActionResult GetAllRides()
        {
            // for testing, you could return dummy data:
            return Ok(new[] { "Ride1", "Ride2" });
        }
    }
}

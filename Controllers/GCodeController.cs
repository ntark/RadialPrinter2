using Microsoft.AspNetCore.Mvc;
using RadialPrinter.Util;

namespace RadialPrinter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GCodeController : ControllerBase
    {
        private readonly ILogger<GCodeController> _logger;

        public GCodeController(ILogger<GCodeController> logger)
        {
            _logger = logger;
        }

        [HttpPost("xyToRad")]
        public string XyToRad()
        {
            return "XyToRad";
        }

        [HttpPost("transform")]
        public string Transform()
        {
            return "Transform";
        }

        [HttpPost("fetchLatest")]
        public async Task<IActionResult> FetchLatest()
        {
            try
            {
                if (string.IsNullOrEmpty(LatestState.LatestPath))
                {
                    return NoContent();
                }

                var instructions = System.IO.File.ReadAllText(LatestState.LatestPath);

                return Ok(instructions);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
    }
}

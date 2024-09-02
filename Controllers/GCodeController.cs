using Microsoft.AspNetCore.Mvc;

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
    }
}

using Microsoft.AspNetCore.Mvc;

namespace RadialPrinter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SVGController : Controller
    {
        private readonly ILogger<GCodeController> _logger;

        public SVGController(ILogger<GCodeController> logger)
        {
            _logger = logger;
        }

        [HttpPost("toGCode")]
        public string ToGCode()
        {
            return "damn";
        }

        [HttpPost("toCenterlineSVG")]
        public string ToCenterlineSVG()
        {
            return "dam2n";
        }
    }
}

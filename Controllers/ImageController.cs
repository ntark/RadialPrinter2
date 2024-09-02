using Microsoft.AspNetCore.Mvc;

namespace RadialPrinter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ImageController : Controller
    {
        private readonly ILogger<GCodeController> _logger;

        public ImageController(ILogger<GCodeController> logger)
        {
            _logger = logger;
        }

        [HttpPost("toSVG")]
        public string ToSVG()
        {
            return "damn";
        }

        [HttpPost("toEdges")]
        public string ToEdges()
        {
            return "dam2n";
        }

        [HttpPost("toRadialFill")]
        public string ToRadialFill()
        {
            return "dam2n";
        }
    }
}

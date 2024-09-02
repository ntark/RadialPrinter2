using Microsoft.AspNetCore.Mvc;
using System.IO;

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
        public async Task<IActionResult> ToSVG(IFormFile file)
        {
            return File(file.OpenReadStream(), "application/octet-stream", file.FileName);
        }

        [HttpPost("toEdges")]
        public string ToEdges()
        {
            return "ToEdges";
        }

        [HttpPost("toRadialFill")]
        public string ToRadialFill()
        {
            return "ToRadialFill";
        }
    }
}

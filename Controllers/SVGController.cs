using Microsoft.AspNetCore.Mvc;
using RadialPrinter.Util;

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
        public async Task<IActionResult> ToGCode(IFormFile file)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var resPath = await PythonAPIHelper.SvgToGCode(filePath);

                return Ok(resPath);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("toCenterlineSVG")]
        public string ToCenterlineSVG()
        {
            return "ToCenterlineSVG";
        }
    }
}

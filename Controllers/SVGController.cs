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
        public async Task<IActionResult> ToGCode(IFormFile file, bool normalize = true)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var resPath = await PythonAPIHelper.SvgToGCode(filePath);

                resPath = normalize ? await GCodeUtil.NormalizeIntoFile(resPath) : resPath;

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
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

using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Diagnostics;
using RadialPrinter.Util;

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

        [HttpPost("toSvg")]
        public async Task<IActionResult> ToSvg(IFormFile file)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var resPath = await PythonAPIHelper.ImageToSvg(filePath);

                return Ok(resPath);
            }
            catch (Exception ex) 
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("toEdges")]
        public async Task<IActionResult> ToEdges(IFormFile file)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var resPath = await PythonAPIHelper.ImageToEdges(filePath);

                return Ok(resPath);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

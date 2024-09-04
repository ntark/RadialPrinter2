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

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
            }
            catch (Exception ex) 
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("toGCode")]
        public async Task<IActionResult> ToGCode(IFormFile file, bool normalize = true)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var svgPath = await PythonAPIHelper.ImageToSvg(filePath);

                var resPath = await PythonAPIHelper.SvgToGCode(svgPath);

                resPath = normalize ? await GCodeUtil.NormalizeIntoFile(resPath) : resPath;

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
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

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("toEdgesSvg")]
        public async Task<IActionResult> ToEdgesSvg(IFormFile file)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var edgesPath = await PythonAPIHelper.ImageToEdges(filePath);

                var resPath = await PythonAPIHelper.ImageToSvg(edgesPath);

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("toEdgesGCode")]
        public async Task<IActionResult> ToEdgesGCode(IFormFile file, bool normalize = true)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var edgesPath = await PythonAPIHelper.ImageToEdges(filePath);

                var svgPath = await PythonAPIHelper.ImageToSvg(edgesPath);

                var resPath = await PythonAPIHelper.SvgToGCode(svgPath);

                resPath = normalize ? await GCodeUtil.NormalizeIntoFile(resPath) : resPath;

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}

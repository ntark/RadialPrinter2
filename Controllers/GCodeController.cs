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

        [HttpPost("setLatestFile")]
        public async Task<IActionResult> SetLatestFile(IFormFile file)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                LatestState.LatestPath = filePath;

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("xyToRad")]
        public async Task<IActionResult> XyToRad(
            IFormFile file,
            double maxDistance = 0.1,
            int radialSteps = -3500,
            int angleSteps = 27800)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var resPath = await GCodeUtil.XyToRad(filePath, maxDistance, radialSteps, angleSteps);

                LatestState.LatestPath = resPath;

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("radToXy")]
        public async Task<IActionResult> YyToRad(
            IFormFile file,
            int radialSteps = -3500,
            int angleSteps = 27800)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var resPath = await GCodeUtil.RadToXy(filePath, radialSteps, angleSteps);

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("fetchLatest")]
        public IActionResult FetchLatest()
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
                Console.WriteLine(ex);
                return BadRequest();
            }
        }

        [HttpPost("drawing")]
        public IActionResult Drawing()
        {
            try
            {
                return Ok(LatestState.Drawing);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest(false);
            }
        }

        [HttpPost("setDrawing")]
        public IActionResult SetDrawing(bool drawing)
        {
            try
            {
                LatestState.Drawing = drawing;
                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest();
            }
        }

        [HttpPost("gcodePreview")]
        public async Task<IActionResult> ParseGCode(IFormFile file)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                var resPath = await GCodeUtil.GCodePreview(filePath);

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

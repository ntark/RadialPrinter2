using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Diagnostics;
using RadialPrinter.Util;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.IO.Pipes;

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

        [HttpPost("toRadialFill")]
        public async Task<IActionResult> ToRadialFill(IFormFile file)
        {
            try
            {
                var filePath = await FileHelper.UploadFile(file);

                using Image<La16> image = Image.Load<La16>(filePath);

                bool[,] im = new bool[image.Width, image.Height];

                int scale = Math.Max(image.Width, image.Height);

                byte minThreshold = 1;
                byte maxThreshold = 250;
                bool invert = false;

                int angle_steps = 1000;
                int radius_steps = 50;

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        var pixelVal = image[x, y].L * image[x, y].A / 255;

                        bool v = pixelVal >= minThreshold && pixelVal <= maxThreshold ? true : false;

                        im[x, y] = invert ? !v : v;
                    }
                }

                using var resImage = new Image<L8>(image.Width, image.Height);

                for (int radius = 0; radius <= radius_steps; radius++)
                {
                    for (int angle = 0; angle < angle_steps; angle++)
                    {
                        double r = (double)radius / radius_steps * scale / 2.0;
                        double a = (double)angle / angle_steps * 2.0 * Math.PI;

                        double x = r * Math.Cos(a) + image.Width / 2.0;
                        double y = r * Math.Sin(a) + image.Height / 2.0;

                        int X = (int)Math.Round(x);
                        int Y = image.Height - (int)Math.Round(y);

                        if (X >= 0 && Y >= 0 && X < image.Width && Y < image.Height && im[X, Y])
                        {
                            resImage[X, Y] = new L8(255);
                        }
                    }
                }

                var resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".png");

                resImage.Save(resPath);

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

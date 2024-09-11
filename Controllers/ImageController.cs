using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Diagnostics;
using RadialPrinter.Util;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.IO.Pipes;
using RadialPrinter.Models;
using RadialPrinter.Enums;

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
        public async Task<IActionResult> ToRadialFill(
            IFormFile file,
            RadialFillFileType fileType,
            byte minThreshold = 1,
            byte maxThreshold = 250,
            bool invert = false,
            int angle_steps = 1000,
            int radius_steps = 50,
            int RADIUS_STEPPER_STEPS = -4000,
            int ANGLE_STEPPER_STEPS = 27800)
        {
            try
            {
                Console.WriteLine($"ToRadialFill on file {file.FileName}");
                var filePath = await FileHelper.UploadFile(file);

                using Image<La16> image = Image.Load<La16>(filePath);

                bool[,] im = new bool[image.Width, image.Height];

                int scale = Math.Max(image.Width, image.Height);

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
                var instructions = new List<RadialInstruction>();

                for (int radius = 0; radius <= radius_steps; radius++)
                {
                    double r = (double)radius / radius_steps * scale / 2.0;
                    int R = radius * RADIUS_STEPPER_STEPS / radius_steps;

                    bool prevLineDrawn = false;
                    int drawStartA = 0;
                    int drawEndA = 0;

                    for (int angle = 0; angle < angle_steps; angle++)
                    {
                        double a = (double)angle / angle_steps * 2.0 * Math.PI;
                        int A = angle * ANGLE_STEPPER_STEPS / angle_steps;

                        double x = r * Math.Cos(a) + image.Width / 2.0;
                        double y = r * Math.Sin(a) + image.Height / 2.0;

                        int X = (int)Math.Round(x);
                        int Y = image.Height - (int)Math.Round(y);

                        var drawThatLine = X >= 0 && Y >= 0 && X < image.Width && Y < image.Height && im[X, Y];

                        if (drawThatLine)
                        {
                            resImage[X, Y] = new L8(255);

                            if (!prevLineDrawn)
                            {
                                drawStartA = A;
                            }
                            drawEndA = A;
                        }
                        else if (prevLineDrawn)
                        {
                            instructions.Add(new(0, R, drawStartA));
                            instructions.Add(new(1, R, drawEndA));
                        }

                        prevLineDrawn = drawThatLine;
                    }

                    if (prevLineDrawn)
                    {
                        instructions.Add(new(0, R, drawStartA));
                        instructions.Add(new(1, R, drawEndA));
                    }
                }

                var resPath = "";

                switch (fileType)
                {
                    case RadialFillFileType.Instructions:
                        {
                            var instructionsText = string.Join("", instructions.Select(x => $"R{x.Mode} {x.R} {x.A}{Environment.NewLine}").ToList());

                            resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".rgcode");

                            await System.IO.File.WriteAllTextAsync(resPath, instructionsText);

                            LatestState.LatestPath = resPath;

                            break;
                        }
                    case RadialFillFileType.Preview:
                        {
                            resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".png");

                            resImage.Save(resPath);
                            break;
                        }
                    default:
                        throw new NotImplementedException();
                }

                Console.WriteLine($"ToRadialFill returning file {resPath}");

                var fileStream = new FileStream(resPath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(resPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return BadRequest(ex.Message);
            }
        }
    }
}

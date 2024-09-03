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

        const string PythonPath = "/usr/local/bin/python3.10";
        const string PythonScriptPath = "/home/opc/workspace/works/c#/RadialPrinter2/Converter/main.py";

        string EdgeGCodeResult(string hash){
            return $"/home/opc/workspace/works/c#/RadialPrinter2/Converter/temp/edges_{hash}.gcode";
        }

        string ImageGCodeResult(string hash){
            return $"/home/opc/workspace/works/c#/RadialPrinter2/Converter/temp/image_{hash}.gcode";
        }

        [HttpPost("toSvg")]
        public async Task<IActionResult> ToSvg(IFormFile file)
        {
            var filePath = Path.Combine("Uploads", file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var resPath = await PythonAPIHelper.ImageToSvg(filePath);

            return Ok(resPath);
        }


        [HttpPost("toGCode")]
        public async Task<IActionResult> ToGCode(IFormFile file)
        {
            // return File(file.OpenReadStream(), "application/octet-stream", file.FileName);
            
            Random rnd = new Random();
            int randomNumber = rnd.Next(1000000, 2000000);
            string hash = $"{randomNumber}";

            await ProcessFile(file, hash);

            var fileStream = new FileStream(ImageGCodeResult(hash), FileMode.Open, FileAccess.Read);
            return File(fileStream, "application/octet-stream", $"Image_{file.FileName}.gcode");
        }

        [HttpPost("toEdgeGCode")]
        public async Task<IActionResult> ToEdgeGCode(IFormFile file)
        {
            Random rnd = new Random();
            int randomNumber = rnd.Next(1000000, 2000000);
            string hash = $"{randomNumber}";

            await ProcessFile(file, hash);

            var fileStream = new FileStream(EdgeGCodeResult(hash), FileMode.Open, FileAccess.Read);
            return File(fileStream, "application/octet-stream", $"Edges_{file.FileName}.gcode");
        }

        private async Task ProcessFile(IFormFile file, string hash) {
            if (file == null || file.Length == 0)
            {
                return;
            }

            var filePath = Path.Combine("Uploads", file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            if (!Directory.Exists("Uploads"))
            {
                Directory.CreateDirectory("Uploads");
            }

            filePath = filePath
                .Replace("\\", "")
                .Replace("\"", "");

            var resp = ExecLine($"{PythonPath} \"{PythonScriptPath}\" \"{filePath}\" {hash}");
        }

        [HttpPost("toRadialFill")]
        public async Task<IActionResult> ToRadialFill()
        {
            return Ok("ToRadialFill");
        }

        private string ExecLine(string command)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo("/bin/bash", $"-c \"{command}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process { StartInfo = processStartInfo })
            {
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                Console.WriteLine("Output:");
                Console.WriteLine(output);

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(error);
                }

                return output;
            }

            return "bruh";
        }
    }
}

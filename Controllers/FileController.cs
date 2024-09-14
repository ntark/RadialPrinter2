using Microsoft.AspNetCore.Mvc;
using RadialPrinter.Util;

namespace RadialPrinter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FileController : Controller
    {
        private readonly ILogger<GCodeController> _logger;

        private readonly string TempPath = "/home/opc/workspace/works/csharp/RadialPrinter2/Converter/temp";
        private readonly string UploadPath = "/home/opc/workspace/works/csharp/RadialPrinter2/Uploads";

        public FileController(ILogger<GCodeController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

                var filePath = "";

                if (System.IO.File.Exists(Path.Join(TempPath, fileName)))
                {
                    filePath = Path.Combine(TempPath, fileName);
                }
                else if (System.IO.File.Exists(Path.Join(UploadPath, fileName)))
                {
                    filePath = Path.Combine(UploadPath, fileName);
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    return NotFound();
                }

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                return File(fileStream, "application/octet-stream", Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}

namespace RadialPrinter.Util
{
    public class FileHelper
    {
        public static async Task<string> UploadFile(IFormFile file)
        {
            var filePath = Path.Combine("Uploads", file.FileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.GetFullPath(filePath);
        }
    }
}

namespace RadialPrinter.Util
{
    public class FileHelper
    {
        public static async Task<string> UploadFile(IFormFile file)
        {
            var randomString = Guid.NewGuid().ToString();

            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var fileExtension = Path.GetExtension(file.FileName);

            var filePath = Path.Combine("Uploads", $"{fileName}_{randomString}{fileExtension}");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.GetFullPath(filePath);
        }

        public static string GetRandomFilePath(string? folderPath, string extension)
        {
            return Path.Join(folderPath, $"{Guid.NewGuid()}{extension}");
        }
    }
}

namespace RadialPrinter.Util
{
    public class PythonAPIHelper
    {
        const string BaseUrl = "http://127.0.0.1:5000/";

        public static async Task<string> ImageToSvg(string filePath)
        {
            using HttpClient client = new HttpClient();

            var url = $"{BaseUrl}imageToSvg?filePath={filePath}";

            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();

            return responseBody;
        }
    }
}

namespace RadialPrinter.Util
{
    public class PythonAPIHelper
    {
        const string BaseUrl = "http://127.0.0.1:5000/";

        public static async Task<string> ImageToSvg(string filePath)
        {
            var url = $"{BaseUrl}imageToSvg?filePath={filePath}";
            
            return await PythonAPIRequest(url);
        }

        public static async Task<string> ImageToEdges(string filePath)
        {
            var url = $"{BaseUrl}imageToEdges?filePath={filePath}";

            return await PythonAPIRequest(url);
        }

        public static async Task<string> SvgToGCode(string filePath)
        {
            var url = $"{BaseUrl}svgToGCode?filePath={filePath}";

            return await PythonAPIRequest(url);
        }

        private static async Task<string> PythonAPIRequest(string url)
        {
            using HttpClient client = new HttpClient();

            var response = await client.GetAsync(url);

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return responseBody;
            }

            throw new Exception(responseBody);
        }
    }
}

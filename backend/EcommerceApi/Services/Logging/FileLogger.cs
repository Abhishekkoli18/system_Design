namespace EcommerceApi.Services.Logging
{
    public class FileLogger : IApplicationLogger
    {
        private readonly string _filePath = Path.Combine("Logs", "app-log.txt");

        public async Task Log(string message)
        {
            // Make sure the Logs folder exists before writing to it
            Directory.CreateDirectory("Logs");

            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

            // AppendAllTextAsync adds to the end of the file without overwriting previous lines
            await File.AppendAllTextAsync(_filePath, line);
        }
    }
}
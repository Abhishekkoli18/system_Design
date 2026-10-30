namespace EcommerceApi.Models
{
    public class LogEntry
    {
        public int Id { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
using EcommerceApi.Data;
using EcommerceApi.Models;

namespace EcommerceApi.Services.Logging
{
    public class DatabaseLogger : IApplicationLogger
    {
        private readonly AppDbContext _db;

        public DatabaseLogger(AppDbContext db)
        {
            _db = db;
        }

        public async Task Log(string message)
        {
            var entry = new LogEntry
            {
                Message = message,
                Timestamp = DateTime.Now
            };

            _db.LogEntries.Add(entry);
            await _db.SaveChangesAsync();
        }
    }
}
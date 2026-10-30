namespace EcommerceApi.Models
{
    // This class maps directly to a database table (called "Users") via EF Core.
    // Every property here becomes a column.
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // We NEVER store the plain password. Only the bcrypt hash of it.
        public string PasswordHash { get; set; } = string.Empty;

        // Simple role string: "User" or "Admin"
        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

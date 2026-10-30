using EcommerceApi.Models;

namespace EcommerceApi.Data
{
    // This class runs ONCE, every time the backend starts up (called from Program.cs).
    // It checks: does an Admin account already exist? If not, create the one
    // hardcoded Admin account. This is the "hardcoded admin" approach your
    // evaluator asked for - there is no public UI to create an Admin account,
    // it simply always exists with these fixed credentials.
    public static class DbSeeder
    {
        // CHANGE THESE if you want different hardcoded admin credentials.
        public const string AdminEmail = "admin@ecommerce.com";
        public const string AdminPassword = "Admin@123";
        public const string AdminFullName = "Site Admin";

        public static void SeedAdmin(AppDbContext db)
        {
            bool adminExists = db.Users.Any(u => u.Role == "Admin");
            if (adminExists)
                return; // already seeded on a previous run - do nothing

            var admin = new User
            {
                FullName = AdminFullName,
                Email = AdminEmail,
                // Same hashing approach as normal registration - BCrypt, never plain text.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
                Role = "Admin"
            };

            db.Users.Add(admin);
            db.SaveChanges();
        }
    }
}

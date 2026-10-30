using EcommerceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceApi.Data
{
    // This class is EF Core's "translator" between your C# code and the actual database.
    // It gets registered in Program.cs and injected wherever it's needed (dependency injection).
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet<User> becomes the "Users" table.
        public DbSet<User> Users { get; set; }

        // DbSet<Product> becomes the "Products" table.
        public DbSet<Product> Products { get; set; }

        // DbSet<CartItem> becomes the "CartItems" table.
        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<LogEntry> LogEntries { get; set; }


        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Email must be unique - stops two accounts registering with the same email.
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // A CartItem belongs to one Product - lets EF Core auto-join
            // Product details when we query CartItems.
            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId);
        }
    }
}

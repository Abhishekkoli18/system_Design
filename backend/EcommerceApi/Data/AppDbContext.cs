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

        public DbSet<Review> Reviews { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponRedemption> CouponRedemptions { get; set; }

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

            modelBuilder.Entity<Review>(e =>
            {
                e.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();     // one review per user per product
                e.HasOne(r => r.Product).WithMany(p => p.Reviews).HasForeignKey(r => r.ProductId);
                e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId);
            });

            modelBuilder.Entity<Coupon>().HasIndex(c => c.Code).IsUnique();
            modelBuilder.Entity<Coupon>().Property(c => c.Value).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(c => c.MaxDiscount).HasPrecision(18, 2);
            modelBuilder.Entity<Coupon>().Property(c => c.MinOrderAmount).HasPrecision(18, 2);

            modelBuilder.Entity<CouponRedemption>().HasIndex(r => new { r.CouponId, r.UserId }).IsUnique();  // once per user
            modelBuilder.Entity<CouponRedemption>().HasOne(r => r.Coupon).WithMany().HasForeignKey(r => r.CouponId);
            modelBuilder.Entity<CouponRedemption>().Property(r => r.DiscountAmount).HasPrecision(18, 2);

            modelBuilder.Entity<Order>().HasIndex(o => o.RazorpayOrderId);   // verify finds the order by this
            modelBuilder.Entity<Order>().Property(o => o.SubTotal).HasPrecision(18, 2);
            modelBuilder.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(18, 2);
        }
    }
}

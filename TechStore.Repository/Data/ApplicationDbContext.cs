using Microsoft.EntityFrameworkCore;
using TechStore.Domain.Models;

namespace TechStore.Repository.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ProductSpecification> ProductSpecifications { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<LoginSession> LoginSessions { get; set; }

        public DbSet<UserDevice> UserDevices { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<KnowledgeItem> KnowledgeItems { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            // THÊM CẤU HÌNH CHO NOTIFICATION Ở ĐÂY:
            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Icon)
                .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Tone)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
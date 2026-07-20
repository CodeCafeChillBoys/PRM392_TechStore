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
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }



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

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.ToTable(table =>
                    table.HasCheckConstraint("CK_Wallets_Balance", "\"Balance\" >= 0"));
                entity.Property(wallet => wallet.Balance).HasPrecision(18, 2);
                entity.HasIndex(wallet => wallet.UserId).IsUnique();
                entity.HasOne(wallet => wallet.User)
                    .WithOne(user => user.Wallet)
                    .HasForeignKey<Wallet>(wallet => wallet.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                entity.ToTable(table =>
                    table.HasCheckConstraint("CK_WalletTransactions_Amount", "\"Amount\" > 0"));
                entity.Property(transaction => transaction.Type).HasConversion<string>();
                entity.Property(transaction => transaction.Status).HasConversion<string>();
                entity.Property(transaction => transaction.Amount).HasPrecision(18, 2);
                entity.Property(transaction => transaction.BalanceBefore).HasPrecision(18, 2);
                entity.Property(transaction => transaction.BalanceAfter).HasPrecision(18, 2);
                entity.HasIndex(transaction => new { transaction.WalletId, transaction.CreatedAt });
                entity.HasIndex(transaction => transaction.VnpayTransactionId)
                    .IsUnique()
                    .HasFilter("\"VnpayTransactionId\" IS NOT NULL");
                entity.HasIndex(transaction => new { transaction.OrderId, transaction.Type })
                    .IsUnique()
                    .HasFilter("\"OrderId\" IS NOT NULL");
                entity.HasOne(transaction => transaction.Wallet)
                    .WithMany(wallet => wallet.Transactions)
                    .HasForeignKey(transaction => transaction.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(transaction => transaction.Order)
                    .WithMany()
                    .HasForeignKey(transaction => transaction.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}

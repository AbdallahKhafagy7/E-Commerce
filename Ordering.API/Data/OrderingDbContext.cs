using Microsoft.EntityFrameworkCore;
using Ordering.API.Domain;

namespace Ordering.API.Data;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(order => order.Id);
            entity.Property(order => order.UserId).HasMaxLength(100).IsRequired();
            entity.Property(order => order.CustomerEmail).HasMaxLength(256).IsRequired();
            entity.Property(order => order.ShippingAddress).HasMaxLength(500).IsRequired();
            entity.Property(order => order.TotalAmount).HasPrecision(18, 2);
            entity.Property(order => order.PaymentTransactionId).HasMaxLength(100);
            entity.Property(order => order.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.HasIndex(order => order.IdempotencyKey).IsUnique();

            entity.HasMany(order => order.Items)
                .WithOne(item => item.Order)
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductNameSnapshot).HasMaxLength(250).IsRequired();
            entity.Property(item => item.UnitPriceSnapshot).HasPrecision(18, 2);
            entity.Property(item => item.LineTotal).HasPrecision(18, 2);
            entity.HasIndex(item => item.ProductId);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Key).HasMaxLength(200).IsRequired();
            entity.Property(record => record.RequestHash).HasMaxLength(128).IsRequired();
            entity.Property(record => record.ResponseJson).IsRequired();
            entity.HasIndex(record => record.Key).IsUnique();
        });
    }
}

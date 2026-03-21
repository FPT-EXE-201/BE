using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Infrastructure.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.Property(s => s.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(s => s.UserId)
            .IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");

        builder.Property(s => s.Plan)
            .IsRequired().HasColumnName("plan")
            .HasConversion<string>().HasMaxLength(20);

        builder.Property(s => s.Price)
            .IsRequired().HasColumnName("price")
            .HasColumnType("DECIMAL(12,2)");

        builder.Property(s => s.StartDate)
            .IsRequired().HasColumnName("start_date")
            .HasColumnType("DATETIME(6)");

        builder.Property(s => s.EndDate)
            .IsRequired().HasColumnName("end_date")
            .HasColumnType("DATETIME(6)");

        builder.Property(s => s.Status)
            .IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20);

        builder.Property(s => s.OrderCode)
            .IsRequired().HasColumnName("order_code");

        builder.Property(s => s.PaymentTransactionId)
            .HasColumnName("payment_transaction_id").HasMaxLength(255);

        // ── Timestamps (BaseEntity) ──
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at").HasColumnType("DATETIME(6)");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("DATETIME(6)");

        builder.Property(s => s.DeletedAt)
            .HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(s => s.IsDeleted);

        // ── Indexes ──
        builder.HasIndex(s => s.UserId).HasDatabaseName("ix_subscriptions_user_id");
        builder.HasIndex(s => s.OrderCode).IsUnique().HasDatabaseName("uk_subscriptions_order_code");
        builder.HasIndex(s => new { s.UserId, s.Status }).HasDatabaseName("ix_subscriptions_user_status");

        // ── Relationships ──
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

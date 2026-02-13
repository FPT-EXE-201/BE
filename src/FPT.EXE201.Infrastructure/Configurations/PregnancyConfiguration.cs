using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PregnancyConfiguration : IEntityTypeConfiguration<Pregnancy>
{
    public void Configure(EntityTypeBuilder<Pregnancy> builder)
    {
        builder.ToTable("pregnancies");

        builder.Property(p => p.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(p => p.UserId)
            .IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");

        builder.Property(p => p.PregnancyNumber)
            .IsRequired().HasColumnName("pregnancy_no");

        builder.Property(p => p.Status)
            .IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.LastMenstrualPeriodDate)
            .HasColumnName("lmp_date").HasColumnType("DATE");

        builder.Property(p => p.ExpectedDeliveryDate)
            .HasColumnName("edd_date").HasColumnType("DATE");

        builder.Property(p => p.EstimatedConceptionDate)
            .HasColumnName("conception_date").HasColumnType("DATE");

        builder.Property(p => p.CurrentGestationalWeek)
            .HasColumnName("current_week");

        builder.Property(p => p.Notes)
            .HasColumnName("notes").HasColumnType("TEXT");

        // ── Nhóm 1: Thông tin bé ──
        builder.Property(p => p.BabyNickname)
            .HasColumnName("baby_nickname").HasMaxLength(100);

        builder.Property(p => p.BabyGender)
            .IsRequired().HasColumnName("baby_gender")
            .HasConversion<string>().HasMaxLength(10)
            .HasDefaultValue(BabyGender.Unknown);

        builder.Property(p => p.PregnancyType)
            .IsRequired().HasColumnName("pregnancy_type")
            .HasConversion<string>().HasMaxLength(15)
            .HasDefaultValue(PregnancyType.Singleton);

        // ── Nhóm 2: Y tế mẹ ──
        builder.Property(p => p.MotherBloodType)
            .HasColumnName("mother_blood_type").HasMaxLength(10);

        builder.Property(p => p.PrePregnancyWeightKg)
            .HasColumnName("pre_pregnancy_weight_kg").HasColumnType("DECIMAL(5,2)");

        builder.Property(p => p.HeightCm)
            .HasColumnName("height_cm").HasColumnType("DECIMAL(5,2)");

        // ── Nhóm 3: Thai sản chuyên sâu ──
        builder.Property(p => p.DueDateSource)
            .IsRequired().HasColumnName("due_date_source")
            .HasConversion<string>().HasMaxLength(15)
            .HasDefaultValue(DueDateSource.LMP);

        builder.Property(p => p.Gravida)
            .HasColumnName("gravida");

        builder.Property(p => p.Para)
            .HasColumnName("para");

        builder.Property(p => p.ActualDeliveryDate)
            .HasColumnName("actual_delivery_date").HasColumnType("DATE");

        builder.Property(p => p.DeliveryMethod)
            .HasColumnName("delivery_method")
            .HasConversion<string>().HasMaxLength(15);

        builder.Property(p => p.CoverImageUrl)
            .HasColumnName("cover_image_url").HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt)
            .HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        // Ignore computed property
        builder.Ignore(p => p.IsDeleted);

        // Unique: 1 user + pregnancy_no
        builder.HasIndex(p => new { p.UserId, p.PregnancyNumber })
            .IsUnique().HasDatabaseName("uk_pregnancies_user_no");

        builder.HasIndex(p => p.UserId).HasDatabaseName("idx_pregnancies_user");
        builder.HasIndex(p => p.Status).HasDatabaseName("idx_pregnancies_status");

        // Relationships
        builder.HasOne(p => p.User)
            .WithMany().HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Conditions)
            .WithOne(c => c.Pregnancy).HasForeignKey(c => c.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Visits)
            .WithOne(v => v.Pregnancy).HasForeignKey(v => v.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tests)
            .WithOne(t => t.Pregnancy).HasForeignKey(t => t.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

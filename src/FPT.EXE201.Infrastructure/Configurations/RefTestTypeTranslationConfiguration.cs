using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefTestTypeTranslationConfiguration : IEntityTypeConfiguration<RefTestTypeTranslation>
{
    public void Configure(EntityTypeBuilder<RefTestTypeTranslation> builder)
    {
        builder.ToTable("ref_test_type_translations");

        builder.HasKey(t => new { t.TestTypeId, t.LanguageCode });

        builder.Property(t => t.TestTypeId).HasColumnName("test_type_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode).IsRequired().HasColumnName("lang_code").HasMaxLength(5).UseCollation("utf8mb4_unicode_ci");
        builder.Property(t => t.DisplayName).IsRequired().HasColumnName("name").HasMaxLength(200);
        builder.Property(t => t.Description).HasColumnName("description").HasColumnType("TEXT");

        builder.HasOne(t => t.TestType)
            .WithMany(tt => tt.Translations).HasForeignKey(t => t.TestTypeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

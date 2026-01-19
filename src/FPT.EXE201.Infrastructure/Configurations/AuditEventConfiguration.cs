using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations
{
    public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
    {
        public void Configure(EntityTypeBuilder<AuditEvent> builder)
        {
            // Table name
            builder.ToTable("audit_events");

            // Primary key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            // Properties
            builder.Property(e => e.ActorUserId)
                .HasColumnName("actor_user_id")
                .IsRequired(false);

            builder.Property(e => e.Action)
                .HasColumnName("action")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.EntityTable)
                .HasColumnName("entity_table")
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(e => e.EntityId)
                .HasColumnName("entity_id")
                .IsRequired(false);

            builder.Property(e => e.BeforeJson)
                .HasColumnName("before_json")
                .HasColumnType("json")
                .IsRequired(false);

            builder.Property(e => e.AfterJson)
                .HasColumnName("after_json")
                .HasColumnType("json")
                .IsRequired(false);

            builder.Property(e => e.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45)
                .IsRequired(false);

            builder.Property(e => e.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(512)
                .IsRequired(false);

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime(3)")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            // Indexes
            builder.HasIndex(e => new { e.EntityTable, e.EntityId, e.CreatedAt })
                .HasDatabaseName("idx_audit_entity");

            builder.HasIndex(e => new { e.ActorUserId, e.CreatedAt })
                .HasDatabaseName("idx_audit_actor_time");

            // Relationships
            builder.HasOne(e => e.ActorUser)
                .WithMany()
                .HasForeignKey(e => e.ActorUserId)
                .HasConstraintName("fk_audit_actor")
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

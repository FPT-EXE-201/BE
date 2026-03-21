using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasOne<StorageFile>()
            .WithMany()
            .HasForeignKey(m => m.AttachmentFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
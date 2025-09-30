using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VideoDownloaderTelegramBot.Models;

namespace VideoDownloaderTelegramBot.Data.Configurations;

/// <summary>
/// Entity configuration for VideoFile
/// </summary>
public class VideoFileConfiguration : IEntityTypeConfiguration<VideoFile>
{
    public void Configure(EntityTypeBuilder<VideoFile> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.FileId).IsUnique();
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => e.IsDeleted);
        builder.HasIndex(e => e.TelegramUserId);

        builder.Property(e => e.FileId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.ContentType)
            .HasMaxLength(100);

        builder.Property(e => e.SourceUrl)
            .HasMaxLength(2000);
    }
}
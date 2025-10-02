using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VideoDownloaderTelegramBot.Db.Entities;

namespace VideoDownloaderTelegramBot.Db.Data.Configurations;

/// <summary>
/// Entity configuration for FileAccessToken
/// </summary>
public class FileAccessTokenConfiguration : IEntityTypeConfiguration<FileAccessToken>
{
    public void Configure(EntityTypeBuilder<FileAccessToken> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.ExpiresAt);
        builder.HasIndex(e => e.IsUsed);

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(e => e.VideoFile)
            .WithMany(v => v.AccessTokens)
            .HasForeignKey(e => e.VideoFileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
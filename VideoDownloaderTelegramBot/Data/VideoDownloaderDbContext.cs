using Microsoft.EntityFrameworkCore;
using VideoDownloaderTelegramBot.Data.Configurations;
using VideoDownloaderTelegramBot.Models;

namespace VideoDownloaderTelegramBot.Data;

/// <summary>
/// Database context for video downloader application
/// </summary>
public class VideoDownloaderDbContext : DbContext
{
    public VideoDownloaderDbContext(DbContextOptions<VideoDownloaderDbContext> options)
        : base(options)
    {
    }

    public DbSet<VideoFile> VideoFiles { get; set; }
    public DbSet<FileAccessToken> FileAccessTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new VideoFileConfiguration());
        modelBuilder.ApplyConfiguration(new FileAccessTokenConfiguration());
    }
}
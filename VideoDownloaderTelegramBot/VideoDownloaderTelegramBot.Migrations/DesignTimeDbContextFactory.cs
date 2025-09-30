using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using VideoDownloaderTelegramBot.Data;

namespace VideoDownloaderTelegramBot.Migrations;

/// <summary>
/// Design-time factory for creating DbContext instances for migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VideoDownloaderDbContext>
{
    public VideoDownloaderDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VideoDownloaderDbContext>();

        // Use a default connection string for design time
        // This will be overridden at runtime with the actual connection string
        optionsBuilder.UseSqlite("Data Source=videodownloader.db");

        return new VideoDownloaderDbContext(optionsBuilder.Options);
    }
}
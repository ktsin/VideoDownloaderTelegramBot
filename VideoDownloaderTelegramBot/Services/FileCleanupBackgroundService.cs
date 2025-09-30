using Microsoft.EntityFrameworkCore;
using VideoDownloaderTelegramBot.Data;

namespace VideoDownloaderTelegramBot.Services;

/// <summary>
/// Background service for cleaning up expired video files
/// </summary>
public class FileCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileCleanupBackgroundService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(10);

    public FileCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<FileCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("File cleanup background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredFilesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during file cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }

        _logger.LogInformation("File cleanup background service stopped");
    }

    /// <summary>
    /// Cleans up expired video files from database and file system
    /// </summary>
    private async Task CleanupExpiredFilesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var storageService = scope.ServiceProvider.GetRequiredService<FileStorageService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VideoDownloaderDbContext>();

        var expiredFiles = await storageService.GetExpiredFilesAsync(cancellationToken);

        if (expiredFiles.Count == 0)
        {
            _logger.LogDebug("No expired files found");
            return;
        }

        _logger.LogInformation("Found {Count} expired files to clean up", expiredFiles.Count);

        foreach (var videoFile in expiredFiles)
        {
            try
            {
                await storageService.DeleteVideoFileAsync(videoFile, cancellationToken);
                _logger.LogInformation("Deleted expired file {FileId}", videoFile.FileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete expired file {FileId}", videoFile.FileId);
            }
        }

        // Clean up expired tokens
        await CleanupExpiredTokensAsync(dbContext, cancellationToken);
    }

    /// <summary>
    /// Removes expired tokens from database
    /// </summary>
    private async Task CleanupExpiredTokensAsync(VideoDownloaderDbContext dbContext, CancellationToken cancellationToken)
    {
        var expiredTokens = await dbContext.FileAccessTokens
            .Where(t => t.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        if (expiredTokens.Count > 0)
        {
            dbContext.FileAccessTokens.RemoveRange(expiredTokens);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Removed {Count} expired tokens", expiredTokens.Count);
        }
    }
}
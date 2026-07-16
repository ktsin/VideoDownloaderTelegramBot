using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using VideoDownloaderTelegramBot.Db.Data;
using VideoDownloaderTelegramBot.Services.Interfaces;

namespace VideoDownloaderTelegramBot.Services;

public class AdminService(
    VideoDownloaderDbContext dbContext,
    FileStorageService fileStorageService,
    ITelegramBotClient botClient,
    IConfiguration configuration,
    ILogger<AdminService> logger) : IAdminService
{
    public Task<List<SubscriberDto>> GetAllSubscribersAsync(CancellationToken ct = default)
        => dbContext.TelegramSubscribers
            .OrderByDescending(s => s.LastSeenAt)
            .Select(s => new SubscriberDto(s.Id, s.TelegramUserId, s.Username, s.FirstName, s.LastName, s.FirstSeenAt, s.LastSeenAt))
            .ToListAsync(ct);

    public Task<List<VideoFileDto>> GetAvailableFilesAsync(CancellationToken ct = default)
        => dbContext.VideoFiles
            .Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new VideoFileDto(f.Id, f.FileId, f.FileName, f.FileSizeBytes, f.SourceUrl, f.TelegramUserId, f.CreatedAt, f.ExpiresAt))
            .ToListAsync(ct);

    public async Task DeleteFileAsync(int videoFileId, CancellationToken ct = default)
    {
        var file = await dbContext.VideoFiles.FindAsync([videoFileId], ct);
        if (file is null)
            return;

        await fileStorageService.DeleteVideoFileAsync(file, ct);
    }

    public Task<DiskUsageDto> GetDiskUsageAsync(CancellationToken ct = default)
    {
        var path = configuration["VideoDownload:Path"] ?? throw new InvalidOperationException("VideoDownload:Path is not configured");

        if (!Directory.Exists(path))
            return Task.FromResult(new DiskUsageDto(0, 0));

        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        var totalBytes = files.Select(f => new FileInfo(f).Length).Sum();
        return Task.FromResult(new DiskUsageDto(totalBytes, files.Length));
    }

    public async Task BroadcastMessageAsync(string text, CancellationToken ct = default)
    {
        var userIds = await dbContext.TelegramSubscribers
            .Where(s => s.IsActive)
            .Select(s => s.TelegramUserId)
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            try
            {
                await botClient.SendMessage(userId, text, cancellationToken: ct);
                await Task.Delay(35, ct); // ~28 msg/s
                logger.LogInformation("Broadcasted to {userId} successfully", userId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send broadcast to user {UserId}", userId);
            }
        }
    }

    public Task SendMessageToUserAsync(long telegramUserId, string text, CancellationToken ct = default)
        => botClient.SendMessage(telegramUserId, text, cancellationToken: ct);
}

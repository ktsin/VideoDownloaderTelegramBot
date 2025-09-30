namespace VideoDownloaderTelegramBot.Services.Interfaces;

public interface IVideoDownloadService
{
    Task<VideoDownloadResult> DownloadVideoAsync(string url, long telegramUserId, CancellationToken cancellationToken = default);
}

public record VideoDownloadResult(bool Success, string? FilePath, string? ErrorMessage, long FileSize = 0, string? DownloadUrl = null);
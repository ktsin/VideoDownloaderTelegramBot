namespace VideoDownloaderTelegramBot.Services;

public interface IVideoDownloadService
{
    Task<VideoDownloadResult> DownloadVideoAsync(string url, CancellationToken cancellationToken = default);
}

public record VideoDownloadResult(bool Success, string? FilePath, string? ErrorMessage, long FileSize = 0);
using VideoDownloaderTelegramBot.Services;

namespace VideoDownloaderTelegramBot.Services.Interfaces;

public interface IAdminService
{
    Task<List<SubscriberDto>> GetAllSubscribersAsync(CancellationToken ct = default);
    Task<List<VideoFileDto>> GetAvailableFilesAsync(CancellationToken ct = default);
    Task DeleteFileAsync(int videoFileId, CancellationToken ct = default);
    Task<DiskUsageDto> GetDiskUsageAsync(CancellationToken ct = default);
    Task BroadcastMessageAsync(string text, CancellationToken ct = default);
    Task SendMessageToUserAsync(long telegramUserId, string text, CancellationToken ct = default);
}

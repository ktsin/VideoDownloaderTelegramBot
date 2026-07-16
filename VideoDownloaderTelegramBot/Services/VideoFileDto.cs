namespace VideoDownloaderTelegramBot.Services;

public record VideoFileDto(
    int      Id,
    string   FileId,
    string   FileName,
    long     FileSizeBytes,
    string   SourceUrl,
    long     TelegramUserId,
    DateTime CreatedAt,
    DateTime ExpiresAt);


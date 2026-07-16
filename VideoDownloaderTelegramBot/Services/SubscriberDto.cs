namespace VideoDownloaderTelegramBot.Services;

public record SubscriberDto(
    int      Id,
    long     TelegramUserId,
    string?  Username,
    string?  FirstName,
    string?  LastName,
    DateTime FirstSeenAt,
    DateTime LastSeenAt);


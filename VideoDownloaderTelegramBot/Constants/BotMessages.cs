namespace VideoDownloaderTelegramBot.Constants;

public static class BotMessages
{
    public const string Welcome = "Welcome to the Video Downloader Bot! Send me a video URL, and I'll download it for you.";
    public const string PromptValidUrl = "Please send a valid video URL.";
    public const string Downloading = "Downloading your video...";
    public const string InvalidUrl = "The URL you provided is not valid. Please check and try again.";
    public const string UnsupportedPlatformPrefix = "Sorry, this platform is not supported.\n\n";
    public const string UnknownDownloadError = "An unknown error occurred during the download.";
    public const string FileTooLargeForTelegram = "The video file is too large to send directly through Telegram (>50MB). You can download it using this link:\n\n{0}";
}


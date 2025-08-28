namespace VideoDownloaderTelegramBot.Services.Interfaces;

public interface IUrlValidationService
{
    bool IsValidUrl(string url);
    bool IsSupportedPlatform(string url);
    string GetSupportedPlatformsList();
}
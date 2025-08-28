namespace VideoDownloaderTelegramBot.Services;

public class UrlValidationService : IUrlValidationService
{
    private readonly List<string> _supportedDomains =
    [
        "youtube.com",
        "youtu.be",
        "tiktok.com",
        "instagram.com",
        "facebook.com",
        "x.com"
    ];

    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public bool IsSupportedPlatform(string url)
    {
        if (!IsValidUrl(url))
            return false;

        var uri = new Uri(url);
        var host = uri.Host.ToLowerInvariant();
        
        return _supportedDomains.Any(domain => 
            host == domain || 
            host == $"www.{domain}" || 
            host.EndsWith($".{domain}"));
    }

    public string GetSupportedPlatformsList()
    {
        return "Supported platforms:\n" +
               "• YouTube (youtube.com, youtu.be)\n" +
               "• TikTok\n" +
               "• Instagram\n" +
               "• Facebook\n" +
               "• X\n" +
               "\nPlease ensure the URL is correct and try again.";
    }
}
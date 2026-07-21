using VideoDownloaderTelegramBot.Constants;
using VideoDownloaderTelegramBot.Services.Interfaces;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace VideoDownloaderTelegramBot.Services;

public class VideoDownloadService : IVideoDownloadService
{
    private readonly YoutubeDL _youtubeDL;
    private readonly ILogger<VideoDownloadService> _logger;
    private readonly FileStorageService _fileStorageService;
    private readonly string _downloadPath;
    private readonly string _downloadSizeLimit;
    private readonly string? _cookiesFilePath;

    public VideoDownloadService(
        YoutubeDL ytdl,
        ILogger<VideoDownloadService> logger,
        IConfiguration configuration,
        FileStorageService fileStorageService)
    {
        _youtubeDL = ytdl;
        _logger = logger;
        _fileStorageService = fileStorageService;
        _downloadPath = configuration["VideoDownload:Path"] ?? Path.Combine(Path.GetTempPath(), "bot_downloads");
        _downloadSizeLimit = configuration["VideoDownload:SizeLimit"] ?? "10G";

        var cookiesSeedPath = configuration["VideoDownload:CookiesFilePath"];
        if (!string.IsNullOrWhiteSpace(cookiesSeedPath))
        {
            var cookiesWorkingPath = configuration["VideoDownload:CookiesWorkingPath"]
                ?? Path.Combine(_downloadPath, "cookies.txt");

            _cookiesFilePath = PrepareCookiesFile(cookiesSeedPath, cookiesWorkingPath);
        }

        Directory.CreateDirectory(_downloadPath);
    }

    /// <summary>
    /// yt-dlp reads the cookies file at startup and writes refreshed session/CSRF cookies
    /// back to it after each run, so it needs a writable copy. To avoid granting write
    /// access to the read-only host-provided seed file, we copy it into a writable working
    /// file (re-copying only when the seed has been updated more recently) and hand yt-dlp
    /// that working copy instead.
    /// </summary>
    private string? PrepareCookiesFile(string seedPath, string workingPath)
    {
        if (!File.Exists(seedPath))
        {
            _logger.LogWarning("VideoDownload:CookiesFilePath is set to {Path} but the file does not exist; continuing without cookies", seedPath);
            return null;
        }

        if (!TryOpenFile(seedPath, FileAccess.Read))
        {
            _logger.LogError("VideoDownload:CookiesFilePath at {Path} could not be read; continuing without cookies", seedPath);
            return null;
        }

        try
        {
            if (!File.Exists(workingPath) || File.GetLastWriteTimeUtc(seedPath) > File.GetLastWriteTimeUtc(workingPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(workingPath)!);
                File.Copy(seedPath, workingPath, overwrite: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to provision writable cookies file at {WorkingPath} from seed {SeedPath}", workingPath, seedPath);
            return null;
        }

        if (!TryOpenFile(workingPath, FileAccess.ReadWrite))
        {
            _logger.LogError("Cookies working file at {Path} is not readable/writable; continuing without cookies", workingPath);
            return null;
        }

        return workingPath;
    }

    private bool TryOpenFile(string path, FileAccess access)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, access, FileShare.ReadWrite);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Failed to open file with {Access} access at {Path}", access, path);
            return false;
        }
    }

    public async Task<VideoDownloadResult> DownloadVideoAsync(string url, long telegramUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new OptionSet
            {
                Format = "bestvideo[height<=1080]+bestaudio/best[height<=1080]/best",
                Output = Path.Combine(_downloadPath, "%(title)s.%(ext)s"),
                RestrictFilenames = true,
                NoPlaylist = true,
                MaxFilesize = _downloadSizeLimit,
                Cookies = _cookiesFilePath
            };

            _logger.LogInformation("Starting video download from {Url}", url);

            var result = await _youtubeDL.RunVideoDataFetch(url, overrideOptions: options, ct: cancellationToken);
            
            if (!result.Success)
            {
                _logger.LogError("Failed to fetch video data: {Error}", string.Join("; ", result.ErrorOutput));
                return new VideoDownloadResult(false, null, "Cannot fetch video data");
            }

            var videoData = result.Data;
            var ext = videoData.Extension ?? "mp4";
            var fileName = $"{Guid.CreateVersion7():D}.{ext}";
            var filePath = Path.GetFullPath(Path.Combine(_downloadPath, fileName));
            options.Output = filePath;

            var downloadResult = await _youtubeDL.RunVideoDownload(url, overrideOptions: options, ct: cancellationToken);

            if (!downloadResult.Success)
            {
                _logger.LogError("Download failed: {Error}", string.Join("; ", downloadResult.ErrorOutput));
                return new VideoDownloadResult(false, null, "Can't download video");
            }

            if (!Path.Exists(filePath))
            {
                _logger.LogError("Downloaded file not found");
                return new VideoDownloadResult(false, null, "Downloaded file not found");
            }

            var fileSize = new FileInfo(filePath).Length;

            _logger.LogInformation("Video downloaded successfully: {FilePath}, Size: {Size} bytes", filePath, fileSize);

            // Check if file exceeds Telegram upload limit
            string? downloadUrl = null;
            if (fileSize > FileConstants.TelegramMaxFileSizeBytes)
            {
                _logger.LogInformation("File size {Size} exceeds Telegram limit, creating download link", fileSize);

                var (_, dlUrl) = await _fileStorageService.SaveVideoFileAsync(
                    filePath,
                    videoData.Title ?? Path.GetFileName(filePath),
                    url,
                    telegramUserId,
                    cancellationToken);

                downloadUrl = dlUrl;
            }

            return new VideoDownloadResult(true, filePath, null, fileSize, downloadUrl);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation(ex, "Download was cancelled");
            return new VideoDownloadResult(false, null, "Download cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading video from {Url}", url);
            return new VideoDownloadResult(false, null, $"Error downloading video has occured: {ex.Message}");
        }
    }
}
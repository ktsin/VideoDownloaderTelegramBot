using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace VideoDownloaderTelegramBot.Services;

public class VideoDownloadService : IVideoDownloadService
{
    private readonly ILogger<VideoDownloadService> _logger;
    private readonly string _downloadPath;
    private readonly string _downloadSizeLimit;

    public VideoDownloadService(ILogger<VideoDownloadService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _downloadPath = configuration["VideoDownload:Path"] ?? Path.Combine(Path.GetTempPath(), "bot_downloads");
        _downloadSizeLimit = configuration["VideoDownload:SizeLimit"] ?? "100M";

        Directory.CreateDirectory(_downloadPath);
    }

    public async Task<VideoDownloadResult> DownloadVideoAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var ytdl = new YoutubeDL();
            
            var options = new OptionSet
            {
                Format = "best[height<=720]/best",
                Output = Path.Combine(_downloadPath, "%(title)s.%(ext)s"),
                RestrictFilenames = true,
                NoPlaylist = true,
                MaxFilesize = _downloadSizeLimit
            };

            _logger.LogInformation("Starting video download from {Url}", url);

            var result = await ytdl.RunVideoDataFetch(url, ct: cancellationToken);
            
            if (!result.Success)
            {
                _logger.LogError("Failed to fetch video data: {Error}", string.Join("; ", result.ErrorOutput));
                return new VideoDownloadResult(false, null, "Cannot fetch video data");
            }

            var videoData = result.Data;
            var title = videoData.Title ?? "video";
            var ext = videoData.Extension ?? "mp4";
            var fileName = $"{SanitizeFileName(title)}.{ext}";
            var filePath = Path.Combine(_downloadPath, fileName);

            var downloadResult = await ytdl.RunVideoDownload(url, overrideOptions: options, ct: cancellationToken);

            if (!downloadResult.Success)
            {
                _logger.LogError("Download failed: {Error}", string.Join("; ", downloadResult.ErrorOutput));
                return new VideoDownloadResult(false, null, "Can't download video");
            }
            
            var downloadedFiles = Directory.GetFiles(_downloadPath, $"{SanitizeFileName(title)}.*")
                .Where(File.Exists)
                .ToArray();

            if (downloadedFiles.Length == 0)
            {
                _logger.LogError("Downloaded file not found");
                return new VideoDownloadResult(false, null, "Downloaded file not found");
            }

            var downloadedFile = downloadedFiles[0];
            var fileSize = new FileInfo(downloadedFile).Length;

            _logger.LogInformation("Video downloaded successfully: {FilePath}, Size: {Size} bytes", downloadedFile, fileSize);

            return new VideoDownloadResult(true, downloadedFile, null, fileSize);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download was cancelled");
            return new VideoDownloadResult(false, null, "Download cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading video from {Url}", url);
            return new VideoDownloadResult(false, null, $"Error downloading video has occured: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 100 ? sanitized[..100] : sanitized;
    }
}
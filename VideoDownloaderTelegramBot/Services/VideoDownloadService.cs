using VideoDownloaderTelegramBot.Services.Interfaces;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace VideoDownloaderTelegramBot.Services;

public class VideoDownloadService : IVideoDownloadService
{
    private readonly YoutubeDL _youtubeDL;
    private readonly ILogger<VideoDownloadService> _logger;
    private readonly string _downloadPath;
    private readonly string _downloadSizeLimit;

    public VideoDownloadService(
        YoutubeDL ytdl,
        ILogger<VideoDownloadService> logger, 
        IConfiguration configuration)
    {
        _youtubeDL = ytdl;
        _logger = logger;
        _downloadPath = configuration["VideoDownload:Path"] ?? Path.Combine(Path.GetTempPath(), "bot_downloads");
        _downloadSizeLimit = configuration["VideoDownload:SizeLimit"] ?? "100M";

        Directory.CreateDirectory(_downloadPath);
    }

    public async Task<VideoDownloadResult> DownloadVideoAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new OptionSet
            {
                Format = "best[height<=720]/best",
                Output = Path.Combine(_downloadPath, "%(title)s.%(ext)s"),
                RestrictFilenames = true,
                NoPlaylist = true,
                MaxFilesize = _downloadSizeLimit
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

            return new VideoDownloadResult(true, filePath, null, fileSize);
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
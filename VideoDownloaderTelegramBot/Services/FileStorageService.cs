using Microsoft.EntityFrameworkCore;
using VideoDownloaderTelegramBot.Db.Data;
using VideoDownloaderTelegramBot.Db.Entities;

namespace VideoDownloaderTelegramBot.Services;

/// <summary>
/// Service for managing file storage operations
/// </summary>
public class FileStorageService
{
    private readonly VideoDownloaderDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileStorageService> _logger;
    private readonly FileTokenService _tokenService;

    public FileStorageService(
        VideoDownloaderDbContext dbContext,
        IConfiguration configuration,
        ILogger<FileStorageService> logger,
        FileTokenService tokenService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Saves a video file to storage and creates a database record
    /// </summary>
    public async Task<(VideoFile videoFile, string downloadUrl)> SaveVideoFileAsync(
        string sourceFilePath,
        string fileName,
        string sourceUrl,
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var hostUrl = _configuration.GetValue<string>("FileStorage:HostUrl")
                      ?? throw new InvalidOperationException("FileStorage:HostUrl is not configured");
        var retentionHours = _configuration.GetValue<int>("FileStorage:FileRetentionHours", 24);

        // Use existing file path (no need to copy)
        var fileInfo = new FileInfo(sourceFilePath);
        var fileId = Guid.NewGuid().ToString("N");
        var storedFileName = Path.GetFileName(sourceFilePath);

        // Create database record
        var videoFile = new VideoFile
        {
            FileId = fileId,
            FileName = fileName,
            FilePath = storedFileName,
            FileSizeBytes = fileInfo.Length,
            ContentType = GetContentType(Path.GetExtension(sourceFilePath)),
            SourceUrl = sourceUrl,
            TelegramUserId = telegramUserId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(retentionHours),
            IsDeleted = false
        };

        _dbContext.VideoFiles.Add(videoFile);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate access token
        var token = await _tokenService.GenerateTokenAsync(videoFile.Id, cancellationToken);

        // Build download URL
        var downloadUrl = $"{hostUrl.TrimEnd('/')}/api/download/{token}";

        _logger.LogInformation("Saved video file {FileId} for user {UserId}, expires at {ExpiresAt}",
            fileId, telegramUserId, videoFile.ExpiresAt);

        return (videoFile, downloadUrl);
    }

    /// <summary>
    /// Gets the file path for a video file
    /// </summary>
    public string GetFilePath(VideoFile videoFile)
    {
        var downloadPath = _configuration.GetValue<string>("VideoDownload:Path")
                           ?? throw new InvalidOperationException("VideoDownload:Path is not configured");
        return Path.Combine(downloadPath, videoFile.FilePath);
    }

    /// <summary>
    /// Deletes a video file from storage and marks it as deleted in database
    /// </summary>
    public async Task DeleteVideoFileAsync(VideoFile videoFile, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(videoFile);

        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {FilePath}", filePath);
            }
        }

        videoFile.IsDeleted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets expired video files that need to be deleted
    /// </summary>
    public async Task<List<VideoFile>> GetExpiredFilesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.VideoFiles
            .Where(f => !f.IsDeleted && f.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Determines content type based on file extension
    /// </summary>
    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".flv" => "video/x-flv",
            ".wmv" => "video/x-ms-wmv",
            ".m4v" => "video/x-m4v",
            _ => "application/octet-stream"
        };
    }
}
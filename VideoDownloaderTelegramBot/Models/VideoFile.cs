namespace VideoDownloaderTelegramBot.Models;

/// <summary>
/// Represents a downloaded video file stored in the system
/// </summary>
public class VideoFile
{
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for the file
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Original file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Relative path to the file in storage
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// MIME type of the file
    /// </summary>
    public string ContentType { get; set; } = "video/mp4";

    /// <summary>
    /// Original URL from which the video was downloaded
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Telegram user ID who requested the download
    /// </summary>
    public long TelegramUserId { get; set; }

    /// <summary>
    /// When the file was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the file should be deleted
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the file has been deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Access tokens for this file
    /// </summary>
    public ICollection<FileAccessToken> AccessTokens { get; set; } = new List<FileAccessToken>();
}
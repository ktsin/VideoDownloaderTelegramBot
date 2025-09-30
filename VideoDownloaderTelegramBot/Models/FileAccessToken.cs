namespace VideoDownloaderTelegramBot.Models;

/// <summary>
/// Represents a temporary access token for downloading a file
/// </summary>
public class FileAccessToken
{
    public int Id { get; set; }

    /// <summary>
    /// The unique access token string
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the video file
    /// </summary>
    public int VideoFileId { get; set; }

    /// <summary>
    /// Navigation property to the video file
    /// </summary>
    public VideoFile VideoFile { get; set; } = null!;

    /// <summary>
    /// When the token was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether the token has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// When the token was used (if applicable)
    /// </summary>
    public DateTime? UsedAt { get; set; }
}
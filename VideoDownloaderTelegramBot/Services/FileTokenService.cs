using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using VideoDownloaderTelegramBot.Data;
using VideoDownloaderTelegramBot.Models;

namespace VideoDownloaderTelegramBot.Services;

/// <summary>
/// Service for generating and validating file access tokens
/// </summary>
public class FileTokenService
{
    private readonly VideoDownloaderDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileTokenService> _logger;

    public FileTokenService(
        VideoDownloaderDbContext dbContext,
        IConfiguration configuration,
        ILogger<FileTokenService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generates a new access token for a video file
    /// </summary>
    public async Task<string> GenerateTokenAsync(int videoFileId, CancellationToken cancellationToken = default)
    {
        var tokenExpirationMinutes = _configuration.GetValue<int>("FileStorage:TokenExpirationMinutes", 60);
        var token = GenerateSecureToken();

        var fileAccessToken = new FileAccessToken
        {
            Token = token,
            VideoFileId = videoFileId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(tokenExpirationMinutes),
            IsUsed = false
        };

        _dbContext.FileAccessTokens.Add(fileAccessToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated access token for video file {VideoFileId}, expires at {ExpiresAt}",
            videoFileId, fileAccessToken.ExpiresAt);

        return token;
    }

    /// <summary>
    /// Validates a token and returns the associated video file if valid
    /// </summary>
    public async Task<VideoFile?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var fileAccessToken = await _dbContext.FileAccessTokens
            .Include(t => t.VideoFile)
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (fileAccessToken == null)
        {
            _logger.LogWarning("Token not found: {Token}", token);
            return null;
        }

        if (fileAccessToken.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Token expired: {Token}, expired at {ExpiresAt}",
                token, fileAccessToken.ExpiresAt);
            return null;
        }

        if (fileAccessToken.VideoFile.IsDeleted)
        {
            _logger.LogWarning("Associated file is deleted for token: {Token}", token);
            return null;
        }

        // Mark token as used
        if (!fileAccessToken.IsUsed)
        {
            fileAccessToken.IsUsed = true;
            fileAccessToken.UsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return fileAccessToken.VideoFile;
    }

    /// <summary>
    /// Generates a cryptographically secure random token
    /// </summary>
    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
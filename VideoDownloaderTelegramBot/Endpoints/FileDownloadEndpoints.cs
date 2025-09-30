using VideoDownloaderTelegramBot.Services;

namespace VideoDownloaderTelegramBot.Endpoints;

/// <summary>
/// Endpoints for file download operations
/// </summary>
public static class FileDownloadEndpoints
{
    /// <summary>
    /// Maps file download endpoints to the application
    /// </summary>
    public static IEndpointRouteBuilder MapFileDownloadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/download");

        group.MapGet("/{token}", HandleFileDownloadAsync)
            .WithName("DownloadFile")
            .WithOpenApi()
            .Produces<FileStreamHttpResult>(StatusCodes.Status200OK)
            .Produces<ProblemHttpResult>(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>
    /// Handles file download request with token validation
    /// </summary>
    private static async Task<IResult> HandleFileDownloadAsync(
        string token,
        FileTokenService tokenService,
        FileStorageService storageService,
        CancellationToken cancellationToken)
    {
        var videoFile = await tokenService.ValidateTokenAsync(token, cancellationToken);

        if (videoFile == null)
        {
            return Results.NotFound(new { error = "Invalid or expired token" });
        }

        var filePath = storageService.GetFilePath(videoFile);

        if (!File.Exists(filePath))
        {
            return Results.NotFound(new { error = "File not found" });
        }

        return Results.File(
            filePath,
            videoFile.ContentType,
            videoFile.FileName,
            enableRangeProcessing: true);
    }
}
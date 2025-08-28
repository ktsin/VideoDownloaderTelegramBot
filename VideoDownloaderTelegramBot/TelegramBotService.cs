using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VideoDownloaderTelegramBot.Services;
using SystemFile = System.IO.File;

namespace VideoDownloaderTelegramBot;

public class TelegramBotService(
    ITelegramBotClient botClient,
    IUrlValidationService urlValidationService,
    IVideoDownloadService videoDownloadService,
    ILogger<TelegramBotService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Telegram bot service");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        await botClient.ReceiveAsync(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message || message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        logger.LogInformation("Received message '{MessageText}' from chat {ChatId}", messageText, chatId);

        if (messageText.StartsWith("/start"))
        {
            var welcomeText = "Welcome to the Video Downloader Bot! Send me a video URL, and I'll download it for you.";
            await botClient.SendMessage(chatId, welcomeText, cancellationToken: cancellationToken);
            return;
        }
        
        if (Uri.IsWellFormedUriString(messageText, UriKind.Absolute))
        {
            await botClient.SendMessage(chatId, "Downloading your video...", cancellationToken: cancellationToken);

            var textValidation = urlValidationService.IsValidUrl(messageText);

            if (!textValidation)
            {
                await botClient.SendMessage(chatId, "The URL you provided is not valid. Please check and try again.", cancellationToken: cancellationToken);
                return;
            }
            
            var platformSupport = urlValidationService.IsSupportedPlatform(messageText);
            if (!platformSupport)
            {
                var supportedPlatforms = urlValidationService.GetSupportedPlatformsList();
                await botClient.SendMessage(chatId, $"Sorry, this platform is not supported.\n\n{supportedPlatforms}", cancellationToken: cancellationToken);
                return;
            }
            
            var downloadResult = await videoDownloadService.DownloadVideoAsync(messageText, cancellationToken);
            if (downloadResult is { Success: true, FilePath: not null })
            {
                await using var fileStream = new FileStream(downloadResult.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileName = Path.GetFileName(downloadResult.FilePath);
                await botClient.SendVideo(
                    chatId: chatId,
                    video: InputFile.FromStream(fileStream),
                    caption: fileName,
                    cancellationToken: cancellationToken);

                try
                {
                    SystemFile.Delete(downloadResult.FilePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete temporary file {FilePath}", downloadResult.FilePath);
                }
            }
            else
            {
                var errorMessage = downloadResult.ErrorMessage ?? "An unknown error occurred during the download.";
                await botClient.SendMessage(chatId, $"Download failed: {errorMessage}", cancellationToken: cancellationToken);
            }
        }
        else
        {
            await botClient.SendMessage(chatId, "Please send a valid video URL.", cancellationToken: cancellationToken);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error occurred in Telegram bot");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Telegram bot service");
        await base.StopAsync(cancellationToken);
    }
}
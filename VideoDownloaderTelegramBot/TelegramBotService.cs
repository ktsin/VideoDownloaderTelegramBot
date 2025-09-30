using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VideoDownloaderTelegramBot.Commands;

namespace VideoDownloaderTelegramBot;

public class TelegramBotService(
    ITelegramBotClient botClient,
    IEnumerable<IMessageCommand> commands,
    ILogger<TelegramBotService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Telegram bot service");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.ChatMember]
        };

        await botClient.ReceiveAsync(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } _ } message)
            return;

        var chatId = message.Chat.Id;
        logger.LogInformation("Received message '{MessageText}' from chat {ChatId}", message.Text, chatId);

        foreach (var command in commands)
        {
            if (!command.CanHandle(message))
                continue;

            var platformSupport = urlValidationService.IsSupportedPlatform(messageText);
            if (!platformSupport)
            {
                var supportedPlatforms = urlValidationService.GetSupportedPlatformsList();
                await client.SendMessage(chatId,
                    $"Sorry, this platform is not supported.\n\n{supportedPlatforms}",
                    messageThreadId: startMessage.MessageThreadId,
                    cancellationToken: cancellationToken);
                return;
            }
            
            var downloadResult = await videoDownloadService.DownloadVideoAsync(messageText, chatId, cancellationToken);
            if (downloadResult is { Success: true })
            {
                // If file is too large, send download link
                if (downloadResult.DownloadUrl != null)
                {
                    await client.SendMessage(
                        chatId,
                        $"✅ Video downloaded successfully!\n\n" +
                        $"The file is too large to send directly via Telegram (size: {downloadResult.FileSize / (1024.0 * 1024.0):F2} MB).\n\n" +
                        $"📥 Download link (valid for 1 hour):\n{downloadResult.DownloadUrl}",
                        messageThreadId: startMessage.MessageThreadId,
                        cancellationToken: cancellationToken);
                }
                // Send file directly via Telegram
                else if (downloadResult.FilePath != null)
                {
                    await using (var fileStream = new FileStream(downloadResult.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        await client.SendVideo(
                            chatId: chatId,
                            video: InputFile.FromStream(fileStream),
                            messageThreadId: startMessage.MessageThreadId,
                            cancellationToken: cancellationToken);
                    }

                    try
                    {
                        SystemFile.Delete(downloadResult.FilePath);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to delete temporary file {FilePath}", downloadResult.FilePath);
                    }
                }
            }
            else
            {
                var errorMessage = downloadResult.ErrorMessage ?? "An unknown error occurred during the download.";
                await client.SendMessage(
                    chatId,
                    $"Download failed: {errorMessage}",
                    messageThreadId: startMessage.MessageThreadId,
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            await client.SendMessage(chatId, "Please send a valid video URL.", cancellationToken: cancellationToken);

            await command.HandleAsync(client, message, cancellationToken);
            return;
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
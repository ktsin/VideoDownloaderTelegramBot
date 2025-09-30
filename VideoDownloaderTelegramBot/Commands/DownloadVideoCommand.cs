using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VideoDownloaderTelegramBot.Constants;
using VideoDownloaderTelegramBot.Services;
using VideoDownloaderTelegramBot.Services.Interfaces;

namespace VideoDownloaderTelegramBot.Commands;

public class DownloadVideoCommand(
    IUrlValidationService urlValidationService,
    IVideoDownloadService videoDownloadService,
    FileStorageService fileStorageService,
    ILogger<DownloadVideoCommand> logger
) : IMessageCommand
{
    public bool CanHandle(Message message) => message.Text != null && Uri.IsWellFormedUriString(message.Text, UriKind.Absolute);

    public async Task HandleAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        var chatId = message.Chat.Id;
        var startMessage = await client.SendMessage(chatId, BotMessages.Downloading, cancellationToken: cancellationToken);

        var messageText = message.Text!;
        var textValidation = urlValidationService.IsValidUrl(messageText);

        if (!textValidation)
        {
            await client.SendMessage(
                chatId,
                BotMessages.InvalidUrl,
                messageThreadId: startMessage.MessageThreadId,
                cancellationToken: cancellationToken);
            return;
        }

        var platformSupport = urlValidationService.IsSupportedPlatform(messageText);
        if (!platformSupport)
        {
            var supportedPlatforms = urlValidationService.GetSupportedPlatformsList();
            await client.SendMessage(chatId,
                $"{BotMessages.UnsupportedPlatformPrefix}{supportedPlatforms}",
                messageThreadId: startMessage.MessageThreadId,
                cancellationToken: cancellationToken);
            return;
        }

        var downloadResult = await videoDownloadService.DownloadVideoAsync(messageText, cancellationToken);
        if (downloadResult is { Success: true, FilePath: not null })
        {
            var fileInfo = new FileInfo(downloadResult.FilePath);

            // Check if file is larger than Telegram's limit (50 MB)
            if (fileInfo.Length > FileConstants.TelegramMaxFileSizeBytes)
            {
                // Store file and generate download link
                var (videoFile, downloadUrl) = await fileStorageService.SaveVideoFileAsync(
                    downloadResult.FilePath,
                    Path.GetFileName(downloadResult.FilePath),
                    messageText,
                    message.From?.Id ?? 0,
                    cancellationToken);

                await client.SendMessage(
                    chatId,
                    string.Format(BotMessages.FileTooLargeForTelegram, downloadUrl),
                    messageThreadId: startMessage.MessageThreadId,
                    cancellationToken: cancellationToken);
            }
            else
            {
                // Send directly through Telegram
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
                    File.Delete(downloadResult.FilePath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete temporary file {FilePath}", downloadResult.FilePath);
                }
            }
        }
        else
        {
            var errorMessage = downloadResult.ErrorMessage ?? BotMessages.UnknownDownloadError;
            await client.SendMessage(
                chatId,
                $"Download failed: {errorMessage}",
                messageThreadId: startMessage.MessageThreadId,
                cancellationToken: cancellationToken);
        }
    }
}


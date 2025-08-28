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
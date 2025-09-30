using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using VideoDownloaderTelegramBot.Commands;

namespace VideoDownloaderTelegramBot;

public class TelegramBotService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramBotService> logger)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram bot service");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message, UpdateType.ChatMember]
        };

        await _botClient.ReceiveAsync(
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
        _logger.LogInformation("Received message '{MessageText}' from chat {ChatId}", message.Text, chatId);

        // Create scope for all commands
        await using var scope = _scopeFactory.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetServices<IMessageCommand>();

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
        _logger.LogError(exception, "Error occurred in Telegram bot");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Telegram bot service");
        await base.StopAsync(cancellationToken);
    }
}
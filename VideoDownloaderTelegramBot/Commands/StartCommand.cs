using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using VideoDownloaderTelegramBot.Constants;

namespace VideoDownloaderTelegramBot.Commands;

public class StartCommand : IMessageCommand
{
    public bool CanHandle(Message message) => message.Text?.StartsWith("/start") == true;

    public Task HandleAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        return client.SendMessage(message.Chat.Id, BotMessages.Welcome, cancellationToken: cancellationToken);
    }
}


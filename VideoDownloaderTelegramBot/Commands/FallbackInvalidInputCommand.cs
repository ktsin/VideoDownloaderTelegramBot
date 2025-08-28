using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using VideoDownloaderTelegramBot.Constants;

namespace VideoDownloaderTelegramBot.Commands;

public class FallbackInvalidInputCommand : IMessageCommand
{
    public bool CanHandle(Message message) => true; // last-resort fallback

    public Task HandleAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        return client.SendMessage(message.Chat.Id, BotMessages.PromptValidUrl, cancellationToken: cancellationToken);
    }
}


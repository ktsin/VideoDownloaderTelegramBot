using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace VideoDownloaderTelegramBot.Commands;

public interface IMessageCommand
{
    bool CanHandle(Message message);
    Task HandleAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken);
}


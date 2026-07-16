using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using VideoDownloaderTelegramBot.Constants;
using VideoDownloaderTelegramBot.Db.Data;
using VideoDownloaderTelegramBot.Db.Entities;

namespace VideoDownloaderTelegramBot.Commands;

public class StartCommand(
    VideoDownloaderDbContext dbContext,
    ILogger<StartCommand> logger)
    : IMessageCommand
{
    public bool CanHandle(Message message) => message
        .Text
        ?.Equals("/start", StringComparison.InvariantCultureIgnoreCase) == true;

    public async Task HandleAsync(ITelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        try
        {
            await UpsertSubscriberAsync(message, cancellationToken);
            await client.SendMessage(message.Chat.Id, BotMessages.Welcome, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling /start command for chat {ChatId}", message.Chat.Id);
            await client.SendMessage(message.Chat.Id, BotMessages.ErrorOccured, cancellationToken: cancellationToken);
        }
    }

    private async Task UpsertSubscriberAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From?.Id ?? message.Chat.Id;
        var now = DateTime.UtcNow;

        var existing = await dbContext
            .TelegramSubscribers
            .FirstOrDefaultAsync(s => s.TelegramUserId == userId, cancellationToken);

        if (existing is null)
        {
            dbContext.TelegramSubscribers.Add(new TelegramSubscriber
            {
                TelegramUserId = userId,
                Username = message.From?.Username,
                FirstName = message.From?.FirstName,
                LastName = message.From?.LastName,
                FirstSeenAt = now,
                LastSeenAt = now,
                IsActive = true
            });
        }
        else
        {
            existing.Username = message.From?.Username;
            existing.FirstName = message.From?.FirstName;
            existing.LastName = message.From?.LastName;
            existing.LastSeenAt = now;
            existing.IsActive = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Upserted subscriber {UserId} for chat {ChatId}; PII: {Username} -- {FirstName} {LastName}",
            userId,
            message.Chat.Id,
            message.From?.Username,
            message.From?.FirstName,
            message.From?.LastName);
    }
}
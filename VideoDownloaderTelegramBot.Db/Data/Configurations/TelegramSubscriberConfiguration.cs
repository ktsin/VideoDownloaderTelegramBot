using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VideoDownloaderTelegramBot.Db.Entities;

namespace VideoDownloaderTelegramBot.Db.Data.Configurations;

public class TelegramSubscriberConfiguration : IEntityTypeConfiguration<TelegramSubscriber>
{
    public void Configure(EntityTypeBuilder<TelegramSubscriber> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TelegramUserId).IsUnique();

        builder.Property(e => e.Username).HasMaxLength(100);
        builder.Property(e => e.FirstName).HasMaxLength(200);
        builder.Property(e => e.LastName).HasMaxLength(200);
    }
}

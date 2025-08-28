using Telegram.Bot;
using VideoDownloaderTelegramBot;
using VideoDownloaderTelegramBot.Services;

var builder = WebApplication.CreateSlimBuilder(args);

var botToken = builder.Configuration["TelegramBot:Token"] ?? 
    throw new InvalidOperationException("Telegram bot token is not configured");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddSingleton<IUrlValidationService, UrlValidationService>();
builder.Services.AddSingleton<IVideoDownloadService, VideoDownloadService>();


var app = builder.Build();

await app.RunAsync();
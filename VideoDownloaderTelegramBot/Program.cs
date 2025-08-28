using Telegram.Bot;
using VideoDownloaderTelegramBot;
using VideoDownloaderTelegramBot.Services;
using VideoDownloaderTelegramBot.Services.Interfaces;
using YoutubeDLSharp;

var builder = WebApplication.CreateSlimBuilder(args);

var botToken = builder.Configuration["TelegramBot:Token"] ?? 
    throw new InvalidOperationException("Telegram bot token is not configured");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddHostedService<TelegramBotService>();
builder.Services.AddSingleton<IUrlValidationService, UrlValidationService>();
builder.Services.AddSingleton<IVideoDownloadService, VideoDownloadService>();
builder.Services.AddSingleton<YoutubeDL>(_ =>
{
    var ytdlPath = builder.Configuration["YoutubeDL:Path"] ?? "yt-dlp";
    
    var ytdlwrapper = new YoutubeDL
    {
        YoutubeDLPath = ytdlPath
    };

    return ytdlwrapper;
});


var app = builder.Build();

await app.RunAsync();
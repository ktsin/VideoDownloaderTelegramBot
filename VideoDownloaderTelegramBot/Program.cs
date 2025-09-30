using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using VideoDownloaderTelegramBot;
using VideoDownloaderTelegramBot.Commands;
using VideoDownloaderTelegramBot.Data;
using VideoDownloaderTelegramBot.Endpoints;
using VideoDownloaderTelegramBot.Services;
using VideoDownloaderTelegramBot.Services.Interfaces;
using YoutubeDLSharp;

var builder = WebApplication.CreateBuilder(args);

var botToken = builder.Configuration["TelegramBot:Token"] ??
    throw new InvalidOperationException("Telegram bot token is not configured");

// Configure database
var databasePath = builder.Configuration["FileStorage:DatabasePath"]
    ?? throw new InvalidOperationException("FileStorage:DatabasePath is not configured");
Directory.CreateDirectory(databasePath);
var connectionString = $"Data Source={Path.Combine(databasePath, "videodownloader.db")}";

builder.Services.AddDbContext<VideoDownloaderDbContext>(options =>
    options.UseSqlite(connectionString));

// Register services
builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddHostedService<TelegramBotService>();

builder.Services.AddHostedService<FileCleanupBackgroundService>();

// Register commands
builder.Services.AddScoped<IMessageCommand, StartCommand>();
builder.Services.AddScoped<IMessageCommand, DownloadVideoCommand>();
builder.Services.AddScoped<IMessageCommand, FallbackInvalidInputCommand>();
builder.Services.AddSingleton<IUrlValidationService, UrlValidationService>();
builder.Services.AddScoped<IVideoDownloadService, VideoDownloadService>();
builder.Services.AddScoped<FileTokenService>();
builder.Services.AddScoped<FileStorageService>();
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

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VideoDownloaderDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Map endpoints
app.MapFileDownloadEndpoints();

await app.RunAsync();
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.FluentUI.AspNetCore.Components;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using VideoDownloaderTelegramBot;
using VideoDownloaderTelegramBot.Auth;
using VideoDownloaderTelegramBot.Commands;
using VideoDownloaderTelegramBot.Components;
using VideoDownloaderTelegramBot.Db.Data;
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
    options.UseSqlite(connectionString,
        providerOptions =>
            providerOptions.MigrationsAssembly(typeof(VideoDownloaderTelegramBot.Migrations.AssemblyMoniker)
                .Assembly)));

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
        YoutubeDLPath = ytdlPath,
        
        
    };

    return ytdlwrapper;
});


// Admin credentials — fail-fast at startup
var adminUser = builder.Configuration["INIT_WEB_ADMIN_MASTER_USER"];
if (string.IsNullOrWhiteSpace(adminUser))
    throw new InvalidOperationException("INIT_WEB_ADMIN_MASTER_USER is not set");
var adminPwd = builder.Configuration["INIT_WEB_ADMIN_MASTER_PWD"];
if (string.IsNullOrWhiteSpace(adminPwd))
    throw new InvalidOperationException("INIT_WEB_ADMIN_MASTER_PWD is not set");
builder.Services.AddSingleton(new AdminCredentials(adminUser, adminPwd));

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath         = "/login";
        options.AccessDeniedPath  = "/login";
        options.ExpireTimeSpan    = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly   = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite   = SameSiteMode.Strict;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Blazor Server + FluentUI
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

// Admin service
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VideoDownloaderDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Map endpoints
app.MapFileDownloadEndpoints();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Login: static form POST — avoids Blazor/SignalR HttpContext limitation
app.MapPost("/login", async (
    HttpContext context,
    AdminCredentials creds,
    [FromForm] string username,
    [FromForm] string password) =>
{
    if (username == creds.Username && password == creds.Password)
    {
        var claims = new[] { new Claim(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
        return Results.Redirect("/admin");
    }
    return Results.Redirect("/login?error=1");
});

app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

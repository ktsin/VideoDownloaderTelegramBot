FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

# Install python3, pip, and youtube-dl
RUN apt update && apt install -y \
    python3-full \
    python3-pip \
    ffmpeg \
    && pip3 install --break-system-packages -U --pre "yt-dlp[default]"

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["VideoDownloaderTelegramBot/VideoDownloaderTelegramBot.csproj", "VideoDownloaderTelegramBot/"]
RUN dotnet restore "VideoDownloaderTelegramBot/VideoDownloaderTelegramBot.csproj"
COPY . .
WORKDIR "/src/VideoDownloaderTelegramBot"
RUN dotnet build "./VideoDownloaderTelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VideoDownloaderTelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Create directory for database
RUN mkdir -p /app/storage/database

COPY --from=publish /app/publish .

# Volume mount for database persistence
VOLUME ["/app/storage/database"]

ENTRYPOINT ["dotnet", "VideoDownloaderTelegramBot.dll"]

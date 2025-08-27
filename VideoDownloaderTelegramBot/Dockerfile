FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install python3, pip, and youtube-dl
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    && pip3 install youtube-dl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoDownloaderTelegramBot.dll"]

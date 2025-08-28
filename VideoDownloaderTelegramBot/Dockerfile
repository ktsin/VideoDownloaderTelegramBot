FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install python3, pip, and youtube-dl
RUN apt update && apt install -y \
    python3-full \
    python3-pip \
    && pip3 install --break-system-packages youtube-dl

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

RUN apt-get update && apt-get install -y \
    clang \
    zlib1g-dev

WORKDIR /src
COPY ["VideoDownloaderTelegramBot/VideoDownloaderTelegramBot.csproj", "VideoDownloaderTelegramBot/"]
RUN dotnet restore "VideoDownloaderTelegramBot/VideoDownloaderTelegramBot.csproj"
COPY . .
WORKDIR "/src/VideoDownloaderTelegramBot"
RUN dotnet build "./VideoDownloaderTelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG RUNTIME_IDENTIFIER=linux-arm64
RUN dotnet publish "./VideoDownloaderTelegramBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -r $RUNTIME_IDENTIFIER --self-contained true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VideoDownloaderTelegramBot.dll"]

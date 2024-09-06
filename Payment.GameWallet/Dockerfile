FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Payment.GameWallet/Payment.GameWallet.csproj", "Payment.GameWallet/"]
COPY ["GameWallet.Core/GameWallet.Core.csproj", "GameWallet.Core/"]
COPY ["GameWallet.Client/GameWallet.Client.csproj", "GameWallet.Client/"]
RUN dotnet restore "./Payment.GameWallet/./Payment.GameWallet.csproj"
COPY . .
WORKDIR "/src/Payment.GameWallet"
RUN dotnet build "./Payment.GameWallet.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Payment.GameWallet.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.GameWallet.dll"]
ARG CI_REGISTRY_IMAGE

FROM $CI_REGISTRY_IMAGE:builder as deps

RUN mkdir -p /src/packages

FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0-alpine AS builder
ARG BUILD_CONFIGURATION=Release
ENV NUGET_PACKAGES=/src/packages

COPY --from=deps $NUGET_PACKAGES/. $NUGET_PACKAGES/

WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Payment.GameWallet/Payment.GameWallet.csproj", "Payment.GameWallet/"]
COPY ["GameWallet.Core/GameWallet.Core.csproj", "GameWallet.Core/"]
COPY ["GameWallet.Client/GameWallet.Client.csproj", "GameWallet.Client/"]
RUN dotnet restore -v n -r linux-x64 "./Payment.GameWallet/./Payment.GameWallet.csproj"
COPY . .
WORKDIR "/src/Payment.GameWallet"
RUN dotnet build "./Payment.GameWallet.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM builder AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -r linux-x64 --self-contained false "./Payment.GameWallet.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.GameWallet.dll"]
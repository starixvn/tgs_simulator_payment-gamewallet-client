FROM hub.lumigame.com/base/dotnet/core/aspnet:8.0-alpine-24h1.1 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.BigPayz/Tgs.Payment.BigPayz.csproj", "Tgs.Payment.BigPayz/"]
COPY ["BigPayz.Core/BigPayz.Core.csproj", "BigPayz.Core/"]
COPY ["BigPayz.Client/BigPayz.Client.csproj", "BigPayz.Client/"]
RUN dotnet restore "./Tgs.Payment.BigPayz/./Tgs.Payment.BigPayz.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.BigPayz"
RUN dotnet build "./Tgs.Payment.BigPayz.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Tgs.Payment.BigPayz.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.BigPayz.dll"]
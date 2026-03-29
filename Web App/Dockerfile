# Etap 1 — build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TelemetryNotifier.csproj ./
RUN dotnet restore TelemetryNotifier.csproj

COPY . ./
RUN dotnet publish TelemetryNotifier.csproj -c Release -o /app/publish

# Etap 2 — runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5050

ENTRYPOINT ["dotnet", "TelemetryNotifier.dll"]


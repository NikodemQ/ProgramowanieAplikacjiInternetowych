# Etap 1 — build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY TelemetryWorker.csproj ./
RUN dotnet restore TelemetryWorker.csproj

COPY . ./
RUN dotnet publish TelemetryWorker.csproj -c Release -o /app/publish

# Etap 2 — runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TelemetryWorker.dll"]


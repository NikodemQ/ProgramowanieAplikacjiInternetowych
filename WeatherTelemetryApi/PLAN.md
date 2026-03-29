# Plan implementacji: Weather Telemetry API

## Stack
- **ASP.NET Core 9** Web API
- **RabbitMQ** (publikacja na wielu kanałach)
- **Swagger / Swashbuckle**
- **Docker + docker-compose**

## Architektura przepływu

```
[POST /api/weather] → [RequestLoggingMiddleware] → [WeatherController]
    → [RabbitMqPublisher] → Base64 + HMAC-SHA256 → [RabbitMQ Exchange: weather]
        → routing key: weather.indoor / weather.outdoor / weather.station
```

---

## Iteracje

### ✅ Iteracja 1 — Szkielet projektu
- [x] Katalog `WeatherTelemetryApi/`, plik `WeatherTelemetryApi.sln`
- [x] Projekt `WeatherTelemetryApi/WeatherTelemetryApi.csproj` (ASP.NET Web API, .NET 9)
- [x] Minimalny `Program.cs`
- [x] `appsettings.json` z sekcjami `RabbitMQ`, `Hmac`, `Channels`
- [x] `.gitignore` dla .NET

**Commit:** `feat: projekt szkielet ASP.NET Web API`

---

### ✅ Iteracja 2 — Modele danych
- [x] `Models/WeatherReading.cs` — pola: `DeviceId`, `Channel`, `Temperature`, `Pressure`, `Humidity`, `Timestamp` z DataAnnotations
- [x] `Models/WeatherPayload.cs` — pola: `Base64Data`, `Checksum`, `Channel`, `CreatedAt`
- [x] `Models/RabbitMqSettings.cs` — mapowanie sekcji `RabbitMQ` z `appsettings.json` (w tym słownik `Channels`)

**Commit:** `feat: modele danych WeatherReading, WeatherPayload, RabbitMqSettings`

---

### ✅ Iteracja 3 — Swagger
- [x] Dodać pakiet NuGet `Swashbuckle.AspNetCore` do `.csproj`
- [x] Skonfigurować `AddSwaggerGen` i `UseSwaggerUI` w `Program.cs`
- [x] Włączyć generowanie XML docs w `.csproj` (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`)
- [x] Przekazać plik XML do Swaggera (`IncludeXmlComments`)

**Commit:** `feat: konfiguracja Swaggera`

---

### ✅ Iteracja 4 — Middleware logowania żądań
- [x] Stworzyć `Middleware/RequestLoggingMiddleware.cs`
  - loguje: metodę HTTP, ścieżkę, ciało żądania (buforowane), status odpowiedzi, czas wykonania
- [x] Zarejestrować middleware w `Program.cs` (`app.UseMiddleware<RequestLoggingMiddleware>()`)

**Commit:** `feat: RequestLoggingMiddleware`

---

### ✅ Iteracja 5 — Serwis RabbitMQ Publisher
- [x] Stworzyć `Services/IRabbitMqPublisher.cs` — interfejs z metodą `PublishAsync(WeatherReading reading)`
- [x] Stworzyć `Services/RabbitMqPublisher.cs`:
  - wstrzykuje `IOptions<RabbitMqSettings>` i `IOptions<HmacSettings>`
  - przy starcie deklaruje exchange `weather` typu `direct`
  - dla każdego odczytu:
    1. serializuje `WeatherReading` do JSON
    2. enkoduje do Base64 → `Base64Data`
    3. generuje HMAC-SHA256 z `Base64Data` kluczem z `appsettings.json` → `Checksum`
    4. buduje `WeatherPayload`
    5. publikuje JSON payloadu na exchange `weather` z routing key z `Channels[channel]`
  - rzuca `InvalidOperationException` gdy kanał nie istnieje w konfiguracji
- [x] Stworzyć `Models/HmacSettings.cs` — mapowanie sekcji `Hmac` (`SecretKey`)
- [x] Zarejestrować serwis jako `Singleton` w `Program.cs`

**Commit:** `feat: serwis RabbitMqPublisher z Base64 i HMAC`

---

### ✅ Iteracja 6 — Kontroler i endpoint
- [x] Stworzyć `Controllers/WeatherController.cs`
  - endpoint `POST /api/weather` przyjmuje `WeatherReading`
  - walidacja `ModelState` → `400 Bad Request` z detalami błędów
  - walidacja czy `Channel` istnieje w konfiguracji → `400 Bad Request`
  - wywołanie `IRabbitMqPublisher.PublishAsync`
  - odpowiedź `202 Accepted` z potwierdzeniem
  - obsługa wyjątków → `500 Internal Server Error`
- [x] Pełne adnotacje Swagger: `/// <summary>`, `[ProducesResponseType]`

**Commit:** `feat: WeatherController POST /api/weather`

---

### Iteracja 7 — Docker i docker-compose
- [ ] Stworzyć `WeatherTelemetryApi/Dockerfile` (multi-stage: `sdk` → `aspnet`)
- [ ] Uzupełnić `docker-compose.yml` w głównym katalogu:
  - serwis `rabbitmq` — obraz `rabbitmq:3-management`, porty `5672`, `15672`
  - serwis `weather-api` — build z `Dockerfile`, zależność od `rabbitmq`, zmienne środowiskowe przekazujące konfigurację
- [ ] Dodać `healthcheck` dla RabbitMQ

**Commit:** `feat: Dockerfile i docker-compose`

---

## Struktura katalogów (docelowa)

```
WeatherTelemetryApi/
├── WeatherTelemetryApi.sln
├── .gitignore
├── Dockerfile
├── PLAN.md
└── WeatherTelemetryApi/
    ├── WeatherTelemetryApi.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    ├── Controllers/
    │   └── WeatherController.cs
    ├── Middleware/
    │   └── RequestLoggingMiddleware.cs
    ├── Models/
    │   ├── WeatherReading.cs
    │   ├── WeatherPayload.cs
    │   ├── RabbitMqSettings.cs
    │   └── HmacSettings.cs
    └── Services/
        ├── IRabbitMqPublisher.cs
        └── RabbitMqPublisher.cs
```

---

## Konfiguracja (appsettings.json)

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "weather",
    "Channels": {
      "indoor":  "weather.indoor",
      "outdoor": "weather.outdoor",
      "station": "weather.station"
    }
  },
  "Hmac": {
    "SecretKey": "super-secret-hmac-key-change-me-in-production"
  }
}
```

---

## Przykładowy payload żądania

```json
{
  "deviceId": "device-001",
  "channel": "indoor",
  "temperature": 21.5,
  "pressure": 1013.25,
  "humidity": 65.0,
  "timestamp": "2026-03-29T12:00:00Z"
}
```

## Przykładowy payload w kolejce (WeatherPayload)

```json
{
  "base64Data": "eyJkZXZpY2VJZCI6ImRldmljZS0wMDEiLCAuLi59",
  "checksum": "abc123...HMAC-SHA256-base64...",
  "channel": "indoor",
  "createdAt": "2026-03-29T12:00:00Z"
}
```


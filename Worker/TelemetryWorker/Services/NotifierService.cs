using System.Text;
using System.Text.Json;
using TelemetryWorker.Models;

namespace TelemetryWorker.Services;

/// <summary>
/// Wysyła powiadomienia HTTP do TelemetryNotifier (Web App) po każdym przetworzonym odczycie.
/// </summary>
public class NotifierService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _notifierUrl;
    private readonly ILogger<NotifierService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NotifierService(IConfiguration config, ILogger<NotifierService> logger)
    {
        _logger = logger;
        _notifierUrl = config["Notifier:Url"] ?? "http://localhost:5050";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_notifierUrl),
            Timeout = TimeSpan.FromSeconds(5)
        };

        _logger.LogInformation("NotifierService zainicjalizowany: url={Url}", _notifierUrl);
    }

    /// <summary>
    /// Wysyła odczyt pogodowy do TelemetryNotifier, który rozgłasza go przez SignalR.
    /// </summary>
    public async Task NotifyAsync(WeatherReading reading, string channel)
    {
        try
        {
            var notification = new
            {
                level = GetLevel(reading),
                message = $"Odczyt z {reading.DeviceId}: temp={reading.Temperature}°C, ciśnienie={reading.Pressure}hPa, wilgotność={reading.Humidity}%",
                checkName = $"Telemetria · {channel}",
                measurement = "weather",
                time = reading.Timestamp.ToString("o"),
                isSystem = false
            };

            var json = JsonSerializer.Serialize(notification, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/webhook/telemetry", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Notifier] Powiadomienie wysłane: device={DeviceId}, channel={Channel}",
                    reading.DeviceId, channel);
            }
            else
            {
                _logger.LogWarning("[Notifier] HTTP {StatusCode} z TelemetryNotifier", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Nie rzucamy wyjątku — powiadomienie jest opcjonalne, nie blokuje pipeline'u
            _logger.LogWarning(ex, "[Notifier] Nie udało się wysłać powiadomienia do TelemetryNotifier");
        }
    }

    /// <summary>
    /// Określa poziom alertu na podstawie wartości odczytu.
    /// </summary>
    private static string GetLevel(WeatherReading reading)
    {
        if (reading.Temperature > 45 || reading.Temperature < -40)
            return "crit";
        if (reading.Temperature > 35 || reading.Temperature < -20 ||
            reading.Humidity > 95 || reading.Humidity < 10)
            return "warn";
        return "info";
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}


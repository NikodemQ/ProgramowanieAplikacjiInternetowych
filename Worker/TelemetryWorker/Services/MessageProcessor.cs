using System.Text;
using System.Text.Json;
using TelemetryWorker.Models;

namespace TelemetryWorker.Services;

/// <summary>
/// Przetwarza surową wiadomość JSON z RabbitMQ na WeatherPayload + WeatherReading.
/// </summary>
public class MessageProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializuje JSON z kolejki do WeatherPayload, a następnie dekoduje Base64Data do WeatherReading.
    /// </summary>
    public (WeatherPayload Payload, WeatherReading Reading) Process(string messageJson)
    {
        var payload = JsonSerializer.Deserialize<WeatherPayload>(messageJson, JsonOptions)
                      ?? throw new InvalidOperationException("Nie udało się zdeserializować WeatherPayload.");

        var readingBytes = Convert.FromBase64String(payload.Base64Data);
        var readingJson = Encoding.UTF8.GetString(readingBytes);

        var reading = JsonSerializer.Deserialize<WeatherReading>(readingJson, JsonOptions)
                      ?? throw new InvalidOperationException("Nie udało się zdeserializować WeatherReading z Base64Data.");

        return (payload, reading);
    }
}


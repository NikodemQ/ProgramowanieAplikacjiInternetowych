namespace WeatherTelemetryApi.Models;

/// <summary>
/// Wiadomość wysyłana do kolejki RabbitMQ.
/// Zawiera dane pogodowe zakodowane w Base64 oraz checksum HMAC-SHA256.
/// </summary>
public class WeatherPayload
{
    /// <summary>
    /// Dane pomiarowe (JSON WeatherReading) zakodowane w Base64.
    /// </summary>
    public string Base64Data { get; set; } = string.Empty;

    /// <summary>
    /// Podpis HMAC-SHA256 wyliczony z Base64Data, zakodowany w Base64.
    /// Służy do weryfikacji integralności danych po stronie Workera.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// Kanał, z którego pochodzi wiadomość (np. indoor, outdoor).
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Czas utworzenia payloadu (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


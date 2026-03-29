namespace TelemetryWorker.Models;

/// <summary>
/// Wiadomość odbierana z kolejki RabbitMQ — odpowiada WeatherPayload z WeatherTelemetryApi.
/// </summary>
public class WeatherPayload
{
    public string Base64Data { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}


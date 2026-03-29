namespace TelemetryWorker.Models;

/// <summary>
/// Odczyt pogodowy zdekodowany z Base64 — odpowiada WeatherReading z WeatherTelemetryApi.
/// </summary>
public class WeatherReading
{
    public string DeviceId { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public double Humidity { get; set; }
    public DateTime Timestamp { get; set; }
}


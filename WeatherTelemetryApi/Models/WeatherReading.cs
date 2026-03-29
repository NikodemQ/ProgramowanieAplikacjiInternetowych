using System.ComponentModel.DataAnnotations;

namespace WeatherTelemetryApi.Models;

/// <summary>
/// Pojedynczy odczyt danych pogodowych z urządzenia pomiarowego.
/// </summary>
public class WeatherReading
{
    /// <summary>
    /// Unikalny identyfikator urządzenia pomiarowego.
    /// </summary>
    /// <example>device-001</example>
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Kanał publikacji (np. indoor, outdoor, station).
    /// Musi odpowiadać kluczowi zdefiniowanemu w RabbitMQ:Channels w appsettings.json.
    /// </summary>
    /// <example>indoor</example>
    [Required]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Temperatura w stopniach Celsjusza.
    /// </summary>
    /// <example>21.5</example>
    [Required]
    [Range(-90.0, 60.0)]
    public double Temperature { get; set; }

    /// <summary>
    /// Ciśnienie atmosferyczne w hPa.
    /// </summary>
    /// <example>1013.25</example>
    [Required]
    [Range(870.0, 1085.0)]
    public double Pressure { get; set; }

    /// <summary>
    /// Wilgotność względna w procentach (0–100).
    /// </summary>
    /// <example>65.0</example>
    [Required]
    [Range(0.0, 100.0)]
    public double Humidity { get; set; }

    /// <summary>
    /// Znacznik czasu pomiaru. Jeśli nie podano, zostanie ustawiony na czas UTC serwera.
    /// </summary>
    /// <example>2026-03-29T12:00:00Z</example>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


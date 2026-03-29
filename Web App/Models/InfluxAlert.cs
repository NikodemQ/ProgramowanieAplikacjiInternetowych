using System.Text.Json.Serialization;

namespace TelemetryNotifier.Models;

/// <summary>
/// Model payloadu webhooka wysyłanego przez InfluxDB 2.x przy wyzwoleniu alertu.
/// InfluxDB wysyła POST JSON na skonfigurowany URL endpointu.
/// </summary>
public class InfluxAlertPayload
{
    [JsonPropertyName("_check_id")]
    public string CheckId { get; set; } = string.Empty;

    [JsonPropertyName("_check_name")]
    public string CheckName { get; set; } = string.Empty;

    [JsonPropertyName("_message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Poziom alertu: ok | info | warn | crit
    /// </summary>
    [JsonPropertyName("_level")]
    public string Level { get; set; } = "info";

    [JsonPropertyName("_time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("_source_measurement")]
    public string SourceMeasurement { get; set; } = string.Empty;

    [JsonPropertyName("_type")]
    public string Type { get; set; } = string.Empty;

    // Dodatkowe wartości pomiarowe – dynamiczne pola z InfluxDB
    [JsonPropertyName("values")]
    public Dictionary<string, object>? Values { get; set; }
}

/// <summary>
/// Uproszczony model alertu wysyłanego do klientów przez SignalR.
/// </summary>
public class AlertNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string CheckName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = "info";
    public string Time { get; set; } = string.Empty;
    public string Measurement { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = false;
}
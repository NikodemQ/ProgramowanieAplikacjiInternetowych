namespace WeatherTelemetryApi.Models;

/// <summary>
/// Ustawienia podpisu HMAC-SHA256.
/// Mapowane z sekcji "Hmac" w appsettings.json.
/// </summary>
public class HmacSettings
{
    /// <summary>
    /// Klucz tajny używany do generowania podpisu HMAC-SHA256.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;
}


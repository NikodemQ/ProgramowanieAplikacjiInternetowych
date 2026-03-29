namespace WeatherTelemetryApi.Models;

/// <summary>
/// Ustawienia połączenia z brokerem RabbitMQ.
/// Mapowane z sekcji "RabbitMQ" w appsettings.json.
/// </summary>
public class RabbitMqSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Nazwa exchange'a na którym publikowane są wiadomości.
    /// </summary>
    public string ExchangeName { get; set; } = "weather";

    /// <summary>
    /// Słownik kanałów: klucz to nazwa kanału (np. "indoor"),
    /// wartość to routing key (np. "weather.indoor").
    /// </summary>
    public Dictionary<string, string> Channels { get; set; } = new();
}


using WeatherTelemetryApi.Models;

namespace WeatherTelemetryApi.Services;

/// <summary>
/// Interfejs serwisu publikującego odczyty pogodowe na RabbitMQ.
/// </summary>
public interface IRabbitMqPublisher
{
    /// <summary>
    /// Publikuje odczyt pogodowy na właściwym kanale RabbitMQ.
    /// </summary>
    /// <param name="reading">Dane pogodowe do opublikowania.</param>
    Task PublishAsync(WeatherReading reading);
}


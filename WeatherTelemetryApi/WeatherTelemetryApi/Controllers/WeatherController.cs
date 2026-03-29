using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WeatherTelemetryApi.Models;
using WeatherTelemetryApi.Services;

namespace WeatherTelemetryApi.Controllers;

/// <summary>
/// Kontroler obsługujący publikację odczytów pogodowych.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WeatherController : ControllerBase
{
    private readonly IRabbitMqPublisher _publisher;
    private readonly RabbitMqSettings _rabbitSettings;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        IRabbitMqPublisher publisher,
        IOptions<RabbitMqSettings> rabbitOptions,
        ILogger<WeatherController> logger)
    {
        _publisher = publisher;
        _rabbitSettings = rabbitOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Przyjmuje odczyt pogodowy i publikuje go na odpowiednim kanale RabbitMQ.
    /// </summary>
    /// <param name="reading">Dane pogodowe z urządzenia pomiarowego.</param>
    /// <returns>Potwierdzenie przyjęcia wiadomości.</returns>
    /// <response code="202">Wiadomość została przyjęta i opublikowana na RabbitMQ.</response>
    /// <response code="400">Nieprawidłowe dane wejściowe lub nieznany kanał.</response>
    /// <response code="500">Błąd wewnętrzny serwera podczas publikacji.</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] WeatherReading reading)
    {
        // Walidacja ModelState (DataAnnotations)
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // Walidacja kanału
        if (!_rabbitSettings.Channels.ContainsKey(reading.Channel))
        {
            ModelState.AddModelError(nameof(reading.Channel),
                $"Nieznany kanał '{reading.Channel}'. Dostępne kanały: {string.Join(", ", _rabbitSettings.Channels.Keys)}.");
            return ValidationProblem(ModelState);
        }

        try
        {
            await _publisher.PublishAsync(reading);

            _logger.LogInformation(
                "Opublikowano odczyt: device={DeviceId}, channel={Channel}",
                reading.DeviceId, reading.Channel);

            return Accepted(new
            {
                message = "Odczyt pogodowy został przyjęty i opublikowany.",
                deviceId = reading.DeviceId,
                channel = reading.Channel,
                timestamp = reading.Timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas publikacji odczytu: device={DeviceId}, channel={Channel}",
                reading.DeviceId, reading.Channel);

            return Problem(
                detail: "Wystąpił błąd podczas publikowania wiadomości na RabbitMQ.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Błąd publikacji");
        }
    }
}


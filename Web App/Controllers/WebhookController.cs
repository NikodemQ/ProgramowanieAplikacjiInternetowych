using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TelemetryNotifier.Hubs;
using TelemetryNotifier.Models;

namespace TelemetryNotifier.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly IHubContext<AlertHub> _hub;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IHubContext<AlertHub> hub, ILogger<WebhookController> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Endpoint odbierający powiadomienia o nowych odczytach z TelemetryWorker.
    /// Worker → POST http://telemetry-notifier:5050/webhook/telemetry
    /// </summary>
    [HttpPost("telemetry")]
    public async Task<IActionResult> ReceiveTelemetry([FromBody] AlertNotification notification)
    {
        notification.Time ??= DateTime.UtcNow.ToString("o");
        notification.Level = notification.Level?.ToLowerInvariant() ?? "info";

        _logger.LogInformation("[Webhook-Telemetry] Odczyt: {CheckName} – {Message}",
            notification.CheckName, notification.Message);

        await _hub.Clients.All.SendAsync("ReceiveAlert", notification);

        return Ok(new { status = "accepted", alertId = notification.Id });
    }

    /// <summary>
    /// Endpoint odbierający webhooki z InfluxDB.
    /// InfluxDB → POST http://localhost:5050/webhook/influx
    /// </summary>
    [HttpPost("influx")]
    public async Task<IActionResult> ReceiveInfluxAlert([FromBody] InfluxAlertPayload payload)
    {
        _logger.LogInformation(
            "[Webhook] Otrzymano alert: Level={Level} | Check={CheckName} | Msg={Message}",
            payload.Level, payload.CheckName, payload.Message);

        // Budujemy uproszczoną notyfikację dla klientów SignalR
        var notification = new AlertNotification
        {
            CheckName   = string.IsNullOrWhiteSpace(payload.CheckName) ? "InfluxDB Alert" : payload.CheckName,
            Message     = string.IsNullOrWhiteSpace(payload.Message)   ? "Brak treści alertu." : payload.Message,
            Level       = payload.Level.ToLowerInvariant(),
            Time        = string.IsNullOrWhiteSpace(payload.Time)
                            ? DateTime.UtcNow.ToString("o")
                            : payload.Time,
            Measurement = payload.SourceMeasurement,
            IsSystem    = false
        };

        // Rozgłaszamy do WSZYSTKICH podłączonych klientów
        await _hub.Clients.All.SendAsync("ReceiveAlert", notification);

        _logger.LogInformation("[Webhook] Alert rozgłoszony do klientów SignalR (Id={Id})", notification.Id);

        return Ok(new { status = "accepted", alertId = notification.Id });
    }

    /// <summary>
    /// Endpoint testowy – pozwala ręcznie wysłać alert bez InfluxDB (np. przez Postman).
    /// POST http://localhost:5050/webhook/test
    /// Body: { "level": "crit", "message": "Test alertu", "checkName": "Mój test" }
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> SendTestAlert([FromBody] AlertNotification testAlert)
    {
        testAlert.Time ??= DateTime.UtcNow.ToString("o");
        testAlert.Level = testAlert.Level?.ToLowerInvariant() ?? "info";

        _logger.LogInformation("[Webhook-Test] Ręczny alert: {Level} – {Message}", testAlert.Level, testAlert.Message);

        await _hub.Clients.All.SendAsync("ReceiveAlert", testAlert);

        return Ok(new { status = "sent", alertId = testAlert.Id });
    }

    /// <summary>
    /// Health-check – InfluxDB może weryfikować dostępność endpointu.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "ok", time = DateTime.UtcNow });
    }
}

using Microsoft.AspNetCore.SignalR;

namespace TelemetryNotifier.Hubs;

/// <summary>
/// Hub SignalR rozgłaszający alerty telemetryczne do wszystkich podłączonych klientów.
/// Klienci nasłuchują zdarzenia "ReceiveAlert".
/// </summary>
public class AlertHub : Hub
{
    private readonly ILogger<AlertHub> _logger;

    public AlertHub(ILogger<AlertHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("[SignalR] Klient połączony: {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("ReceiveAlert", new
        {
            level    = "info",
            message  = "Połączono z systemem telemetrycznym.",
            checkName = "System",
            time     = DateTime.UtcNow.ToString("o"),
            isSystem = true
        });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("[SignalR] Klient rozłączony: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Metoda wywoływana przez klienta – pozwala klientowi potwierdzić odbiór alertu.
    /// </summary>
    public async Task AcknowledgeAlert(string alertId)
    {
        _logger.LogInformation("[SignalR] Alert {AlertId} potwierdzony przez {ConnectionId}",
            alertId, Context.ConnectionId);
        await Clients.Caller.SendAsync("AlertAcknowledged", alertId);
    }
}
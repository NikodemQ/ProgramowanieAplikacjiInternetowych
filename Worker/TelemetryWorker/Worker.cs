using TelemetryWorker.Services;

namespace TelemetryWorker;

public class Worker : BackgroundService
{
    private readonly RabbitConsumer _consumer;
    private readonly ILogger<Worker> _logger;

    public Worker(RabbitConsumer consumer, ILogger<Worker> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TelemetryWorker uruchamia się...");

        try
        {
            await _consumer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TelemetryWorker zatrzymany.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Krytyczny błąd w TelemetryWorker!");
            throw;
        }
    }
}

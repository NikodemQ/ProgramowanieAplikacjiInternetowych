using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TelemetryWorker.Services;

namespace TelemetryWorker.Services;

/// <summary>
/// Konsument RabbitMQ — nasłuchuje na kolejce, przetwarza wiadomości WeatherPayload,
/// waliduje checksum i zapisuje do InfluxDB.
/// Korzysta z RabbitMQ.Client v7 (pełne async API).
/// </summary>
public class RabbitConsumer : IAsyncDisposable
{
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitConsumer> _logger;
    private readonly MessageProcessor _processor;
    private readonly ChecksumValidator _validator;
    private readonly InfluxService _influx;
    private readonly NotifierService _notifier;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitConsumer(
        IConfiguration config,
        ILogger<RabbitConsumer> logger,
        MessageProcessor processor,
        ChecksumValidator validator,
        InfluxService influx,
        NotifierService notifier)
    {
        _config = config;
        _logger = logger;
        _processor = processor;
        _validator = validator;
        _influx = influx;
        _notifier = notifier;
    }

    /// <summary>
    /// Inicjalizacja asynchroniczna — tworzy połączenie, kanał, exchange, kolejki i bindingi.
    /// </summary>
    private async Task InitAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
            UserName = _config["RabbitMQ:Username"] ?? "guest",
            Password = _config["RabbitMQ:Password"] ?? "guest",
            VirtualHost = _config["RabbitMQ:VirtualHost"] ?? "/"
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        var exchangeName = _config["RabbitMQ:ExchangeName"] ?? "weather";
        var queue = _config["RabbitMQ:Queue"] ?? "telemetry.queue";
        var dlq = _config["RabbitMQ:Dlq"] ?? "telemetry.dlq";

        // Deklaracja exchange — identycznie jak w WeatherTelemetryApi
        await _channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // DLQ — Dead Letter Queue
        await _channel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false);

        // Główna kolejka z DLX
        var args = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", "" },
            { "x-dead-letter-routing-key", dlq }
        };

        await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, arguments: args);

        // Binding kolejki do exchange dla wszystkich routing keys (kanałów)
        var routingKeys = new[] { "weather.indoor", "weather.outdoor", "weather.station" };
        foreach (var routingKey in routingKeys)
        {
            await _channel.QueueBindAsync(queue, exchangeName, routingKey);
            _logger.LogInformation("Kolejka {Queue} zbindowana do exchange {Exchange} z routing key {RoutingKey}",
                queue, exchangeName, routingKey);
        }

        // Prefetch — przetwarzamy 1 wiadomość na raz
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        _logger.LogInformation("RabbitMQ zainicjalizowany: host={Host}, exchange={Exchange}, queue={Queue}",
            factory.HostName, exchangeName, queue);
    }

    /// <summary>
    /// Inicjalizacja z retry — próbuje połączyć się z RabbitMQ do 10 razy co 5s.
    /// </summary>
    private async Task InitWithRetryAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 10;
        const int delayMs = 5000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                stoppingToken.ThrowIfCancellationRequested();
                await InitAsync();
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Próba połączenia z RabbitMQ {Attempt}/{MaxRetries} nieudana. Ponowna próba za {Delay}s...",
                    attempt, maxRetries, delayMs / 1000);

                if (attempt == maxRetries)
                {
                    _logger.LogCritical("Nie udało się połączyć z RabbitMQ po {MaxRetries} próbach.", maxRetries);
                    throw;
                }

                await Task.Delay(delayMs, stoppingToken);
            }
        }
    }

    /// <summary>
    /// Uruchamia nasłuchiwanie na kolejce. Wywoływane z Worker.ExecuteAsync.
    /// </summary>
    public async Task StartAsync(CancellationToken stoppingToken)
    {
        await InitWithRetryAsync(stoppingToken);

        var queue = _config["RabbitMQ:Queue"] ?? "telemetry.queue";

        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageString = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Odebrano wiadomość z RabbitMQ (routing key: {RoutingKey})", ea.RoutingKey);

            try
            {
                var (payload, reading) = _processor.Process(messageString);

                if (!_validator.Validate(payload))
                {
                    throw new InvalidOperationException("Nieprawidłowy checksum HMAC — wiadomość odrzucona.");
                }

                _logger.LogInformation("Checksum OK: device={DeviceId}, channel={Channel}",
                    reading.DeviceId, payload.Channel);

                await _influx.SaveAsync(reading, payload.Channel);
                await _notifier.NotifyAsync(reading, payload.Channel);

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                _logger.LogInformation("ACK: device={DeviceId}", reading.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd przetwarzania wiadomości — NACK → DLQ");
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel!.BasicConsumeAsync(queue, autoAck: false, consumer: consumer);

        _logger.LogInformation("Konsument RabbitMQ uruchomiony, nasłuchuje na kolejce {Queue}", queue);

        // Utrzymuj wątek dopóki CancellationToken nie zostanie anulowany
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Konsument RabbitMQ zatrzymany.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}



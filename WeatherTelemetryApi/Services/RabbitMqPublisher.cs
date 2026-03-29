using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using WeatherTelemetryApi.Models;

namespace WeatherTelemetryApi.Services;

/// <summary>
/// Serwis publikujący odczyty pogodowe na RabbitMQ.
/// Serializuje dane do JSON, koduje w Base64, podpisuje HMAC-SHA256
/// i wysyła na exchange "weather" z odpowiednim routing key.
/// </summary>
public class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly RabbitMqSettings _rabbitSettings;
    private readonly HmacSettings _hmacSettings;
    private readonly ILogger<RabbitMqPublisher> _logger;

    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public RabbitMqPublisher(
        IOptions<RabbitMqSettings> rabbitOptions,
        IOptions<HmacSettings> hmacOptions,
        ILogger<RabbitMqPublisher> logger)
    {
        _rabbitSettings = rabbitOptions.Value;
        _hmacSettings = hmacOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Leniwa inicjalizacja połączenia i kanału RabbitMQ.
    /// Deklaruje exchange "weather" typu direct.
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_channel is not null) return;

        await _initLock.WaitAsync();
        try
        {
            if (_channel is not null) return;

            var factory = new ConnectionFactory
            {
                HostName = _rabbitSettings.Host,
                Port = _rabbitSettings.Port,
                UserName = _rabbitSettings.Username,
                Password = _rabbitSettings.Password,
                VirtualHost = _rabbitSettings.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(
                exchange: _rabbitSettings.ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            _logger.LogInformation(
                "Połączono z RabbitMQ ({Host}:{Port}), exchange: {Exchange}",
                _rabbitSettings.Host,
                _rabbitSettings.Port,
                _rabbitSettings.ExchangeName);
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync(WeatherReading reading)
    {
        if (!_rabbitSettings.Channels.TryGetValue(reading.Channel, out var routingKey))
        {
            throw new InvalidOperationException(
                $"Kanał '{reading.Channel}' nie istnieje w konfiguracji RabbitMQ:Channels.");
        }

        await EnsureInitializedAsync();

        // 1. Serializacja do JSON
        var json = JsonSerializer.Serialize(reading, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // 2. Kodowanie do Base64
        var base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        // 3. HMAC-SHA256
        var checksum = ComputeHmac(base64Data);

        // 4. Budowanie payloadu
        var payload = new WeatherPayload
        {
            Base64Data = base64Data,
            Checksum = checksum,
            Channel = reading.Channel,
            CreatedAt = DateTime.UtcNow
        };

        // 5. Publikacja
        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var body = Encoding.UTF8.GetBytes(payloadJson);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel!.BasicPublishAsync(
            exchange: _rabbitSettings.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body);

        _logger.LogInformation(
            "Opublikowano wiadomość: device={DeviceId}, channel={Channel}, routingKey={RoutingKey}",
            reading.DeviceId,
            reading.Channel,
            routingKey);
    }

    private string ComputeHmac(string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_hmacSettings.SecretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(hash);
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
        _initLock.Dispose();
    }
}


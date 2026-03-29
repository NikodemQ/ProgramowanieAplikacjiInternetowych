namespace TelemetryWorker;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Text;

public class RabbitConsumer
{
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitConsumer> _logger;
    private readonly MessageProcessor _processor;
    private readonly ChecksumValidator _validator;
    private readonly InfluxService _influx;

    private IConnection _connection;
    private IModel _channel;

    public RabbitConsumer(
        IConfiguration config,
        ILogger<RabbitConsumer> logger,
        MessageProcessor processor,
        ChecksumValidator validator,
        InfluxService influx)
    {
        _config = config;
        _logger = logger;
        _processor = processor;
        _validator = validator;
        _influx = influx;

        Init();
    }

    private void Init()
    {
        var factory = new ConnectionFactory()
        {
            HostName = _config["RabbitMQ:Host"],
            UserName = _config["RabbitMQ:User"],
            Password = _config["RabbitMQ:Pass"]
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        var queue = _config["RabbitMQ:Queue"];
        var dlq = _config["RabbitMQ:Dlq"];

        // DLQ
        _channel.QueueDeclare(dlq, true, false, false);

        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-routing-key", dlq }
        };

        _channel.QueueDeclare(queue, true, false, false, args);

        _logger.LogInformation("RabbitMQ initialized");
    }

    public void Start()
    {
        var queue = _config["RabbitMQ:Queue"];

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var messageString = Encoding.UTF8.GetString(body);

            _logger.LogInformation("Message received");

            try
            {
                var msg = _processor.Process(messageString);

                if (!_validator.Validate(msg))
                {
                    throw new Exception("Invalid checksum");
                }

                await _influx.SaveAsync(msg);

                _channel.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("ACK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing failed");

                _channel.BasicNack(ea.DeliveryTag, false, false); // → DLQ
            }
        };

        _channel.BasicConsume(queue, false, consumer);
    }
}
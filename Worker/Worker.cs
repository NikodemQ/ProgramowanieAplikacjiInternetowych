using System.Threading;
using System.Threading.Tasks;

public class Worker : BackgroundService
{
    private readonly RabbitConsumer _consumer;

    public Worker(RabbitConsumer consumer)
    {
        _consumer = consumer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Start();
        return Task.CompletedTask;
    }
}
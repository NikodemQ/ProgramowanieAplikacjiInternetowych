namespace TelemetryWorker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    services.AddSingleton<MessageProcessor>();
    services.AddSingleton<InfluxService>();
    services.AddSingleton<ChecksumValidator>(sp =>
        new ChecksumValidator(context.Configuration["Security:SecretKey"]));
    services.AddSingleton<RabbitConsumer>();
    services.AddHostedService<Worker>();
});

await builder.Build().RunAsync();
using TelemetryWorker;
using TelemetryWorker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Rejestracja serwisów
builder.Services.AddSingleton<MessageProcessor>();
builder.Services.AddSingleton<ChecksumValidator>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var secretKey = config["Hmac:SecretKey"]
                    ?? throw new InvalidOperationException("Brak konfiguracji Hmac:SecretKey");
    return new ChecksumValidator(secretKey);
});
builder.Services.AddSingleton<InfluxService>();
builder.Services.AddSingleton<NotifierService>();
builder.Services.AddSingleton<RabbitConsumer>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

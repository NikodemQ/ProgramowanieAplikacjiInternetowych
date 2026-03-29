using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using TelemetryWorker.Models;

namespace TelemetryWorker.Services;

/// <summary>
/// Serwis zapisujący odczyty pogodowe do InfluxDB 2.x.
/// </summary>
public class InfluxService : IAsyncDisposable
{
    private readonly InfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;
    private readonly ILogger<InfluxService> _logger;

    public InfluxService(IConfiguration config, ILogger<InfluxService> logger)
    {
        _logger = logger;

        var url = config["InfluxDB:Url"] ?? "http://localhost:8086";
        var token = config["InfluxDB:Token"] ?? "";
        _org = config["InfluxDB:Org"] ?? "telemetry";
        _bucket = config["InfluxDB:Bucket"] ?? "weather";

        _client = new InfluxDBClient(url, token);

        _logger.LogInformation("InfluxDB client zainicjalizowany: url={Url}, org={Org}, bucket={Bucket}",
            url, _org, _bucket);
    }

    /// <summary>
    /// Zapisuje odczyt pogodowy jako point do InfluxDB.
    /// </summary>
    public async Task SaveAsync(WeatherReading reading, string channel)
    {
        var point = PointData
            .Measurement("weather")
            .Tag("deviceId", reading.DeviceId)
            .Tag("channel", channel)
            .Field("temperature", reading.Temperature)
            .Field("pressure", reading.Pressure)
            .Field("humidity", reading.Humidity)
            .Timestamp(reading.Timestamp.ToUniversalTime(), WritePrecision.Ms);

        var writeApi = _client.GetWriteApiAsync();
        await writeApi.WritePointAsync(point, _bucket, _org);

        _logger.LogInformation(
            "[InfluxDB] Zapisano: device={DeviceId}, channel={Channel}, temp={Temp}°C, pressure={Press}hPa, humidity={Hum}%",
            reading.DeviceId, channel, reading.Temperature, reading.Pressure, reading.Humidity);
    }

    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        return ValueTask.CompletedTask;
    }
}



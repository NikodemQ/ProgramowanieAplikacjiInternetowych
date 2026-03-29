namespace TelemetryWorker;

using System;
using System.Threading.Tasks;

public class InfluxService
{
    public Task SaveAsync(TelemetryMessage msg)
    {
        Console.WriteLine($"[Influx] Saved: {msg.DeviceId} = {msg.Value}");
        return Task.CompletedTask;
    }
}
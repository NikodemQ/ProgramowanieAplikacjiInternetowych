namespace TelemetryWorker;

using System;
using System.Text;
using System.Text.Json;

public class MessageProcessor
{
    public TelemetryMessage Process(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var json = Encoding.UTF8.GetString(bytes);

        return JsonSerializer.Deserialize<TelemetryMessage>(json);
    }
}
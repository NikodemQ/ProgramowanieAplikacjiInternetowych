namespace TelemetryWorker;

public class TelemetryMessage
{
    public string DeviceId { get; set; }
    public long Timestamp { get; set; }
    public double Value { get; set; }
    public string Checksum { get; set; }
}
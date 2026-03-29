namespace TelemetryWorker;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public class ChecksumValidator
{
    private readonly string _secret;

    public ChecksumValidator(string secret)
    {
        _secret = secret;
    }

    public bool Validate(TelemetryMessage message)
    {
        var clone = new
        {
            message.DeviceId,
            message.Timestamp,
            message.Value
        };

        var json = JsonSerializer.Serialize(clone);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
        var computed = Convert.ToBase64String(hash);

        return computed == message.Checksum;
    }
}
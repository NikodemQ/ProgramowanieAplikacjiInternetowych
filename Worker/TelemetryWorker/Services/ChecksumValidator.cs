using System.Security.Cryptography;
using System.Text;
using TelemetryWorker.Models;

namespace TelemetryWorker.Services;

/// <summary>
/// Waliduje HMAC-SHA256 checksum wiadomości — algorytm identyczny jak w WeatherTelemetryApi.RabbitMqPublisher.
/// HMAC jest obliczany z wartości Base64Data (string → bytes) z użyciem wspólnego klucza.
/// </summary>
public class ChecksumValidator
{
    private readonly byte[] _keyBytes;

    public ChecksumValidator(string secretKey)
    {
        _keyBytes = Encoding.UTF8.GetBytes(secretKey);
    }

    /// <summary>
    /// Sprawdza, czy checksum w payloadzie zgadza się z HMAC-SHA256 obliczonym z Base64Data.
    /// </summary>
    public bool Validate(WeatherPayload payload)
    {
        var dataBytes = Encoding.UTF8.GetBytes(payload.Base64Data);
        var hash = HMACSHA256.HashData(_keyBytes, dataBytes);
        var computed = Convert.ToBase64String(hash);

        return computed == payload.Checksum;
    }
}


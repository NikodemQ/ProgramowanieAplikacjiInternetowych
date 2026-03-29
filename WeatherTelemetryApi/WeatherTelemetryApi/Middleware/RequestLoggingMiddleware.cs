using System.Diagnostics;

namespace WeatherTelemetryApi.Middleware;

/// <summary>
/// Middleware logujący każde przychodzące żądanie HTTP:
/// metodę, ścieżkę, ciało żądania, kod odpowiedzi oraz czas wykonania.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Buforowanie ciała żądania
        context.Request.EnableBuffering();

        string requestBody = string.Empty;
        if (context.Request.ContentLength > 0)
        {
            using var reader = new StreamReader(
                context.Request.Body,
                leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Żądanie: {Method} {Path} | Ciało: {Body}",
            context.Request.Method,
            context.Request.Path,
            string.IsNullOrWhiteSpace(requestBody) ? "(brak)" : requestBody);

        await _next(context);

        stopwatch.Stop();

        _logger.LogInformation(
            "Odpowiedź: {Method} {Path} | Status: {StatusCode} | Czas: {ElapsedMs} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}


using TelemetryNotifier.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Rejestracja serwisów
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Logowanie do konsoli
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Middleware
app.UseCors();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

// Hub SignalR dostępny pod /alertHub
app.MapHub<AlertHub>("/alertHub");

// Domyślna strona
app.MapFallbackToFile("index.html");

app.Logger.LogInformation("TelemetryNotifier uruchomiony na {Url}", 
    builder.Configuration["urls"] ?? "http://localhost:5050");

app.Run();
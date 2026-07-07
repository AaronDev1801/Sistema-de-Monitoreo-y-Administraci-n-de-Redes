using MonitoringPlatform.Models;
using MonitoringPlatform.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<AlmacenamientoMonitoreo>();
builder.Services.AddSingleton<ServicioAlertas>();
builder.Services.AddHttpClient<ServicioSupabase>();
builder.Services.AddSingleton<ServicioSupabase>();
builder.Services.AddHostedService<SimuladorTelemetria>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/telemetry/recent", (AlmacenamientoMonitoreo store, int limit = 20) =>
{
    var telemetria = store.ObtenerTelemetriaReciente(limit)
        .Select(item => new
        {
            deviceName = item.NombreDispositivo,
            category = item.Categoria,
            protocol = item.Protocolo,
            cpuUsage = item.UsoCpu,
            memoryUsage = item.UsoMemoria,
            bandwidthMbps = item.AnchoBandaMbps,
            latencyMs = item.LatenciaMs,
            temperatureC = item.TemperaturaC,
            packetLoss = item.PerdidaPaquetes,
            timestamp = item.Timestamp,
            additionalData = item.DatosAdicionales ?? new Dictionary<string, object>()
        });

    return Results.Ok(telemetria);
});

app.MapGet("/api/alerts/recent", (AlmacenamientoMonitoreo store, int limit = 20) =>
{
    var alertas = store.ObtenerAlertasRecientes(limit)
        .Select(item => new
        {
            deviceName = item.NombreDispositivo,
            severity = item.Severidad,
            title = item.Titulo,
            message = item.Mensaje,
            protocol = item.Protocolo,
            timestamp = item.Timestamp
        });

    return Results.Ok(alertas);
});

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", timestamp = DateTimeOffset.UtcNow }));

app.Run();

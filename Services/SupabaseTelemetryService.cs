using System.Net.Http.Headers;
using System.Net.Http.Json;
using MonitoringPlatform.Models;

namespace MonitoringPlatform.Services;

public sealed class ServicioSupabase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServicioSupabase> _logger;
    private const int MaxRowsPerTable = 10;

    public ServicioSupabase(HttpClient httpClient, IConfiguration configuration, ILogger<ServicioSupabase> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnviarTelemetriaAsync(TelemetriaDispositivo telemetria, CancellationToken cancellationToken)
    {
        var url = _configuration["Supabase:Url"];
        var key = _configuration["Supabase:ApiKey"];

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var payload = new[]
        {
            new
            {
                device_name = telemetria.NombreDispositivo,
                category = telemetria.Categoria,
                protocol = telemetria.Protocolo,
                cpu_usage = telemetria.UsoCpu,
                memory_usage = telemetria.UsoMemoria,
                bandwidth_mbps = telemetria.AnchoBandaMbps,
                latency_ms = telemetria.LatenciaMs,
                temperature_c = telemetria.TemperaturaC,
                packet_loss = telemetria.PerdidaPaquetes,
                created_at = FormatearTimestampParaSupabase(telemetria.Timestamp)
            }
        };

        await InsertAsync(url, key, "telemetry", payload, cancellationToken);
        await EnforceRetentionAsync(url, key, "telemetry", MaxRowsPerTable, cancellationToken);
    }

    public async Task EnviarAlertaAsync(RegistroAlerta alerta, CancellationToken cancellationToken)
    {
        var url = _configuration["Supabase:Url"];
        var key = _configuration["Supabase:ApiKey"];

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        var payload = new[]
        {
            new
            {
                device_name = alerta.NombreDispositivo,
                severity = alerta.Severidad,
                title = alerta.Titulo,
                message = alerta.Mensaje,
                protocol = alerta.Protocolo,
                created_at = FormatearTimestampParaSupabase(alerta.Timestamp)
            }
        };

        await InsertAsync(url, key, "alerts", payload, cancellationToken);
        await EnforceRetentionAsync(url, key, "alerts", MaxRowsPerTable, cancellationToken);
    }

    private static string FormatearTimestampParaSupabase(DateTimeOffset timestamp)
    {
        return timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }

    private async Task InsertAsync(string url, string key, string table, object payload, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{url.TrimEnd('/')}/rest/v1/{table}")
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add("apikey", key);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.Add("Prefer", "return=minimal");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Supabase insert failed for {Table}: {StatusCode} {Body}", table, response.StatusCode, body);
        }
    }

    private async Task EnforceRetentionAsync(string url, string key, string table, int maxRows, CancellationToken cancellationToken)
    {
        var countRequest = new HttpRequestMessage(HttpMethod.Get, $"{url.TrimEnd('/')}/rest/v1/{table}?select=id&limit=10000");
        countRequest.Headers.Add("apikey", key);
        countRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

        using var countResponse = await _httpClient.SendAsync(countRequest, cancellationToken);
        if (!countResponse.IsSuccessStatusCode)
        {
            return;
        }

        var rows = await countResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>(cancellationToken: cancellationToken);
        var currentCount = rows?.Count ?? 0;
        if (currentCount <= maxRows)
        {
            return;
        }

        var excess = currentCount - maxRows;
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-5);
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"{url.TrimEnd('/')}/rest/v1/{table}?created_at=lt.{FormatearTimestampParaSupabase(cutoff)}&limit={excess}");
        deleteRequest.Headers.Add("apikey", key);
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        deleteRequest.Headers.Add("Prefer", "return=minimal");

        using var deleteResponse = await _httpClient.SendAsync(deleteRequest, cancellationToken);
        if (!deleteResponse.IsSuccessStatusCode)
        {
            var body = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Supabase retention cleanup failed for {Table}: {StatusCode} {Body}", table, deleteResponse.StatusCode, body);
        }
    }
}

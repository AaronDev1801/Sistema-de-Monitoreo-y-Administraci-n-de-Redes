using System.Collections.Concurrent;
using MonitoringPlatform.Models;

namespace MonitoringPlatform.Services;

public sealed class AlmacenamientoMonitoreo
{
    private readonly ConcurrentQueue<TelemetriaDispositivo> _telemetria = new();
    private readonly ConcurrentQueue<RegistroAlerta> _alertas = new();
    private readonly int _maxItems;

    public AlmacenamientoMonitoreo(int maxItems = 250)
    {
        _maxItems = maxItems;
    }

    public void AgregarTelemetria(TelemetriaDispositivo telemetria)
    {
        _telemetria.Enqueue(telemetria);
        Recortar(_telemetria, _maxItems);
    }

    public void AgregarAlerta(RegistroAlerta alerta)
    {
        _alertas.Enqueue(alerta);
        Recortar(_alertas, 80);
    }

    public IReadOnlyList<TelemetriaDispositivo> ObtenerTelemetriaReciente(int limit = 20)
    {
        return _telemetria.TakeLast(Math.Min(limit, _telemetria.Count)).ToList();
    }

    public IReadOnlyList<RegistroAlerta> ObtenerAlertasRecientes(int limit = 20)
    {
        return _alertas.TakeLast(Math.Min(limit, _alertas.Count)).ToList();
    }

    public IReadOnlyList<TelemetriaDispositivo> ObtenerUltimaPorDispositivo(int limit = 12)
    {
        return _telemetria
            .GroupBy(item => item.NombreDispositivo)
            .Select(group => group.Last())
            .OrderByDescending(item => item.Timestamp)
            .Take(limit)
            .ToList();
    }

    private static void Recortar<T>(ConcurrentQueue<T> cola, int maxItems)
    {
        while (cola.Count > maxItems)
        {
            cola.TryDequeue(out _);
        }
    }
}

using MonitoringPlatform.Models;

namespace MonitoringPlatform.Services;

public sealed class ServicioAlertas
{
    private readonly AlmacenamientoMonitoreo _almacenamiento;
    private readonly ILogger<ServicioAlertas> _logger;

    public ServicioAlertas(AlmacenamientoMonitoreo almacenamiento, ILogger<ServicioAlertas> logger)
    {
        _almacenamiento = almacenamiento;
        _logger = logger;
    }

    public async Task<RegistroAlerta?> EvaluarAsync(TelemetriaDispositivo telemetria, CancellationToken cancellationToken = default)
    {
        if (telemetria.UsoCpu > 85 || telemetria.UsoMemoria > 90 || telemetria.LatenciaMs > 120 || telemetria.PerdidaPaquetes > 5 || telemetria.TemperaturaC > 45)
        {
            var severidad = telemetria.UsoCpu > 95 || telemetria.TemperaturaC > 55 ? "critical" : "warning";
            var titulo = severidad == "critical" ? "Anomalía crítica detectada" : "Anomalía detectada";
            var mensaje = $"{telemetria.NombreDispositivo} reportó valores fuera de rango mediante {telemetria.Protocolo}.";
            var alerta = new RegistroAlerta(telemetria.NombreDispositivo, severidad, titulo, mensaje, telemetria.Protocolo, telemetria.Timestamp);
            _almacenamiento.AgregarAlerta(alerta);
            _logger.LogWarning("{Titulo} para {Dispositivo}", titulo, telemetria.NombreDispositivo);
            return alerta;
        }

        return null;
    }
}

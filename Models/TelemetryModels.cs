namespace MonitoringPlatform.Models;

public sealed record TelemetriaDispositivo(
    string NombreDispositivo,
    string Categoria,
    string Protocolo,
    double UsoCpu,
    double UsoMemoria,
    double AnchoBandaMbps,
    double LatenciaMs,
    double TemperaturaC,
    double PerdidaPaquetes,
    DateTimeOffset Timestamp = default,
    Dictionary<string, object>? DatosAdicionales = null);

public sealed record RegistroAlerta(
    string NombreDispositivo,
    string Severidad,
    string Titulo,
    string Mensaje,
    string Protocolo,
    DateTimeOffset Timestamp = default);

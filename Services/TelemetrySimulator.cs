using MonitoringPlatform.Models;

namespace MonitoringPlatform.Services;

public sealed class SimuladorTelemetria : BackgroundService
{
    private static readonly string[] Protocolos = ["SNMP", "Syslog", "NetFlow"];
    private readonly AlmacenamientoMonitoreo _almacenamiento;
    private readonly ServicioAlertas _servicioAlertas;
    private readonly ServicioSupabase _servicioSupabase;
    private readonly ILogger<SimuladorTelemetria> _logger;

    public SimuladorTelemetria(AlmacenamientoMonitoreo almacenamiento, ServicioAlertas servicioAlertas, ServicioSupabase servicioSupabase, ILogger<SimuladorTelemetria> logger)
    {
        _almacenamiento = almacenamiento;
        _servicioAlertas = servicioAlertas;
        _servicioSupabase = servicioSupabase;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            GenerarLote();
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private void GenerarLote()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var devices = new[]
        {
            ("Core-Router-01", "Routers"),
            ("Distribution-Switch-01", "Switches"),
            ("Edge-Firewall-01", "Firewalls"),
            ("Office-AP-01", "Access Points"),
            ("Mail-Server-01", "Servidores"),
            ("User-PC-42", "Equipos de usuarios"),
            ("Server-Room-Sensor", "Sensores ambientales del cuarto de servidores")
        };

        foreach (var (deviceName, category) in devices)
        {
            var protocolo = Protocolos[Random.Shared.Next(Protocolos.Length)];
            var (cpu, mem, bw, lat, temp, loss, datos) = GenerarDatosDispositivo(category, protocolo);

            var telemetria = new TelemetriaDispositivo(
                deviceName, category, protocolo, cpu, mem, bw, lat, temp, loss, timestamp, datos);

            _almacenamiento.AgregarTelemetria(telemetria);
            _ = _servicioSupabase.EnviarTelemetriaAsync(telemetria, CancellationToken.None);

            var alerta = _servicioAlertas.EvaluarAsync(telemetria, CancellationToken.None).GetAwaiter().GetResult();
            if (alerta is not null)
                _ = _servicioSupabase.EnviarAlertaAsync(alerta, CancellationToken.None);
        }

        _logger.LogInformation("Lote de telemetría generado en {Timestamp}", timestamp);
    }

    private (double cpu, double mem, double bw, double lat, double temp, double loss, Dictionary<string, object> datos) GenerarDatosDispositivo(string tipo, string protocolo) =>
        tipo switch
        {
            "Routers" => (
                Math.Clamp(45 + Random.Shared.Next(-15, 16), 10, 100),
                Math.Clamp(55 + Random.Shared.Next(-15, 16), 20, 100),
                Math.Clamp(450 + Random.Shared.Next(-100, 101), 100, 1000),
                Math.Clamp(20 + Random.Shared.Next(-8, 9), 5, 100),
                24,
                Math.Clamp(Random.Shared.NextDouble() * 2, 0, 5),
                new()
                {
                    { "traficoEntrante", Random.Shared.Next(1000, 5000) },
                    { "traficoSaliente", Random.Shared.Next(1000, 5000) },
                    { "rutasActivas", Random.Shared.Next(15, 45) },
                    { "intentosSospechosos", Random.Shared.Next(0, 5) }
                }),
            "Switches" => (
                Math.Clamp(50 + Random.Shared.Next(-10, 11), 20, 100),
                Math.Clamp(60 + Random.Shared.Next(-12, 13), 30, 95),
                Math.Clamp(500 + Random.Shared.Next(-80, 81), 150, 900),
                Math.Clamp(5 + Random.Shared.Next(-2, 3), 1, 50),
                22,
                Math.Clamp(Random.Shared.NextDouble() * 0.5, 0, 1),
                new()
                {
                    { "puertosActivos", Random.Shared.Next(18, 48) },
                    { "colisiones", Random.Shared.Next(0, 10) },
                    { "congestion", Random.Shared.Next(0, 80) },
                    { "consumoEnergia", Math.Round(Random.Shared.NextDouble() * 300 + 150, 1) }
                }),
            "Firewalls" => (
                Math.Clamp(60 + Random.Shared.Next(-12, 13), 25, 100),
                Math.Clamp(70 + Random.Shared.Next(-15, 16), 35, 100),
                Math.Clamp(300 + Random.Shared.Next(-50, 51), 80, 600),
                Math.Clamp(30 + Random.Shared.Next(-10, 11), 10, 150),
                26,
                Math.Clamp(Random.Shared.NextDouble() * 1, 0, 2),
                new()
                {
                    { "accesosBloqueados", Random.Shared.Next(10, 100) },
                    { "intentosIntrusión", Random.Shared.Next(0, 20) },
                    { "reglasAplicadas", Random.Shared.Next(50, 200) },
                    { "alertasAmenazas", Random.Shared.Next(0, 10) }
                }),
            "Access Points" => (
                Math.Clamp(35 + Random.Shared.Next(-10, 11), 10, 80),
                Math.Clamp(45 + Random.Shared.Next(-12, 13), 20, 85),
                Math.Clamp(150 + Random.Shared.Next(-40, 41), 30, 300),
                Math.Clamp(15 + Random.Shared.Next(-5, 6), 2, 100),
                23,
                Math.Clamp(Random.Shared.NextDouble() * 3, 0, 8),
                new()
                {
                    { "clientesConectados", Random.Shared.Next(5, 60) },
                    { "intensidadSenial", Random.Shared.Next(-40, -20) },
                    { "canalesUsados", Random.Shared.Next(1, 14) },
                    { "interferencias", Random.Shared.Next(0, 5) }
                }),
            "Servidores" => (
                Math.Clamp(55 + Random.Shared.Next(-18, 19), 15, 95),
                Math.Clamp(70 + Random.Shared.Next(-18, 19), 30, 100),
                Math.Clamp(400 + Random.Shared.Next(-100, 101), 100, 900),
                Math.Clamp(8 + Random.Shared.Next(-3, 4), 2, 50),
                25,
                Math.Clamp(Random.Shared.NextDouble() * 0.3, 0, 1),
                new()
                {
                    { "usoEmDisco", Random.Shared.Next(40, 85) },
                    { "disponibilidadServicios", Random.Shared.Next(95, 100) },
                    { "accesoUsuarios", Random.Shared.Next(10, 200) },
                    { "fallosProcesos", Random.Shared.Next(0, 3) }
                }),
            "Equipos de usuarios" => (
                Math.Clamp(30 + Random.Shared.Next(-15, 16), 5, 90),
                Math.Clamp(50 + Random.Shared.Next(-20, 21), 15, 95),
                Math.Clamp(50 + Random.Shared.Next(-30, 31), 5, 200),
                Math.Clamp(12 + Random.Shared.Next(-5, 6), 1, 100),
                22,
                Math.Clamp(Random.Shared.NextDouble() * 5, 0, 15),
                new()
                {
                    { "antivirus", Random.Shared.Next(90, 100) },
                    { "parchesAplicados", Random.Shared.Next(50, 150) },
                    { "intentosAcceso", Random.Shared.Next(0, 10) },
                    { "fallosHardware", Random.Shared.Next(0, 2) }
                }),
            _ => (
                Math.Clamp(20 + Random.Shared.Next(-8, 9), 5, 50),
                Math.Clamp(30 + Random.Shared.Next(-10, 11), 10, 80),
                10,
                5,
                Math.Clamp(22 + Random.Shared.Next(-5, 6), 18, 35),
                0,
                new()
                {
                    { "temperatura", Math.Round(22 + Random.Shared.NextDouble() * 8, 1) },
                    { "humedad", Random.Shared.Next(30, 70) },
                    { "flujoAire", Random.Shared.Next(100, 500) },
                    { "consumoElectrico", Math.Round(Random.Shared.NextDouble() * 50 + 100, 1) }
                })
        };
}

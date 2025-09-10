using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Observability;

public class HealthCheckMetricsPublisher : IHealthCheckPublisher
{
    private int _selfStatus = 2; // 0=Unhealthy, 1=Degraded, 2=Healthy
    private readonly Dictionary<string, int> _dependencyStatuses = new();

    public HealthCheckMetricsPublisher(IConfiguration config)
    {
        var serviceName = config["ServiceName"] ?? "UnknownService";
        var meter = new Meter("HealthChecksMetrics");

        // Метрика только для self‑health
        meter.CreateObservableGauge(
            "service_health_status",
            () => new Measurement<int>(_selfStatus, new KeyValuePair<string, object?>("service", serviceName)),
            description: "0=Unhealthy, 1=Degraded, 2=Healthy (self‑check only)"
        );

        // Метрика для зависимостей
        meter.CreateObservableGauge(
            "dependency_health_status",
            () => _dependencyStatuses.Select(d =>
                new Measurement<int>(
                    d.Value,
                    new KeyValuePair<string, object?>("service", serviceName),
                    new KeyValuePair<string, object?>("dependency", d.Key)
                )),
            description: "0=Unhealthy, 1=Degraded, 2=Healthy (dependencies)"
        );
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        // Self‑check = проверка с именем "self"
        var selfEntry = report.Entries.FirstOrDefault(e => e.Key == "self");
        if (!selfEntry.Equals(default(KeyValuePair<string, HealthReportEntry>)))
        {
            _selfStatus = selfEntry.Value.Status switch
            {
                HealthStatus.Healthy => 2,
                HealthStatus.Degraded => 1,
                _ => 0
            };
        }
        else
        {
            // Если нет self‑check, считаем сервис Healthy
            _selfStatus = 2;
        }

        // Зависимости = все проверки, кроме self
        _dependencyStatuses.Clear();
        foreach (var entry in report.Entries.Where(e => e.Key != "self"))
        {
            _dependencyStatuses[entry.Key] = entry.Value.Status switch
            {
                HealthStatus.Healthy => 2,
                HealthStatus.Degraded => 1,
                _ => 0
            };
        }

        return Task.CompletedTask;
    }
}


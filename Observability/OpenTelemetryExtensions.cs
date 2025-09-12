using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddMyOpenTelemetry(this IServiceCollection services, string serviceName)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .SetSampler(new AlwaysOnSampler()) // <--- добавили
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.RecordException = true;
                    })
                    .AddOtlpExporter(o => { o.Endpoint = new Uri("http://otel-collector:4317"); });
            })
            .WithMetrics(metricsProviderBuilder =>
            {
                metricsProviderBuilder
                    .SetExemplarFilter(ExemplarFilterType.AlwaysOn) // <--- включаем exemplars
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddMeter(serviceName)
                    .AddMeter("HealthChecksMetrics")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    // ВАЖНО: включаем exemplars на гистограммах
                    .AddView(
                        instrumentName: "*",
                        new ExplicitBucketHistogramConfiguration
                        {
                            // Подбери границы под свои RPS/латентности (пример для HTTP latency)
                            Boundaries = new double[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 }
                        })
                    .AddOtlpExporter((o, m) =>
                    {
                        o.Endpoint = new Uri("http://otel-collector:4317");
                        o.Protocol = OtlpExportProtocol.Grpc;
                        m.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 1000;
                    });
            });

        return services;
    }
}
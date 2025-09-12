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
                    .SetSampler(new AlwaysOnSampler())
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.RecordException = true;
                    })
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri("http://otel-collector:4317");
                        o.Protocol = OtlpExportProtocol.Grpc;
                    });
            })
            .WithMetrics(metricsProviderBuilder =>
            {
                metricsProviderBuilder
                    .SetExemplarFilter(ExemplarFilterType.TraceBased) // Важно! Меняем на TraceBased
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddMeter("HealthChecksMetrics")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddView(
                        instrumentName: "http.server.request.duration",
                        new ExplicitBucketHistogramConfiguration
                        {
                            Boundaries = new double[] { 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10 }
                        })
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri("http://otel-collector:4317");
                        o.Protocol = OtlpExportProtocol.Grpc;
                    });
            });

        return services;
    }
}
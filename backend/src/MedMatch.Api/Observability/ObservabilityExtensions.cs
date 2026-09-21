using System.Diagnostics;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MedMatch.Api.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddMedMatchObservability(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        // 1. Resolve configuration values with sensible defaults and environment fallbacks
        var serviceName = configuration["OTEL_SERVICE_NAME"]
            ?? configuration["OTEL_BACKEND_SERVICE_NAME"]
            ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")
            ?? Environment.GetEnvironmentVariable("OTEL_BACKEND_SERVICE_NAME")
            ?? "medmatch-backend";

        var serviceVersion = configuration["OTEL_SERVICE_VERSION"]
            ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION")
            ?? "1.0.0";

        var baseEndpointStr = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            ?? "https://otel.kevdevs.org";

        // IMPORTANT: keep this as a plain string. Wrapping it in a Uri and
        // interpolating it again re-adds a trailing "/", causing the
        // double-slash bug (".../v1/logs" -> "..//v1/logs").
        var baseTrimmed = baseEndpointStr.TrimEnd('/');

        var logsEndpointStr = configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT")
            ?? $"{baseTrimmed}/v1/logs";

        var tracesEndpointStr = configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT")
            ?? $"{baseTrimmed}/v1/traces";

        var metricsEndpointStr = configuration["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT")
            ?? $"{baseTrimmed}/v1/metrics";

        var token = configuration["OTEL_AUTH_TOKEN"]
            ?? configuration["OTEL_API_KEY"]
            ?? Environment.GetEnvironmentVariable("OTEL_AUTH_TOKEN")
            ?? Environment.GetEnvironmentVariable("OTEL_API_KEY")
            ?? string.Empty;

        var headersStr = configuration["OTEL_EXPORTER_OTLP_HEADERS"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");

        if (string.IsNullOrWhiteSpace(headersStr) && !string.IsNullOrWhiteSpace(token))
        {
            headersStr = $"Authorization=Bearer {token}";
        }

        // Temporary diagnostics - remove once the 401 is confirmed fixed.
        // Do NOT leave this enabled in production (it can leak the token).
        if (builder.Environment.IsDevelopment())
        {
            Console.WriteLine($"[OTEL DEBUG] base endpoint: '{baseTrimmed}'");
            Console.WriteLine($"[OTEL DEBUG] logs endpoint: '{logsEndpointStr}'");
            Console.WriteLine($"[OTEL DEBUG] traces endpoint: '{tracesEndpointStr}'");
            Console.WriteLine($"[OTEL DEBUG] metrics endpoint: '{metricsEndpointStr}'");
            Console.WriteLine($"[OTEL DEBUG] headers configured: {(!string.IsNullOrWhiteSpace(headersStr) ? "yes" : "NO - missing token/headers!")}");
        }

        void ConfigureResource(ResourceBuilder r) =>
            r.AddService(serviceName: serviceName, serviceVersion: serviceVersion)
             .AddAttributes(new Dictionary<string, object>
             {
                 ["environment"] = builder.Environment.EnvironmentName,
                 ["deployment.environment"] = builder.Environment.EnvironmentName,
                 ["host.name"] = Environment.MachineName
             });

        // 2. OpenTelemetry Logging
        builder.Logging.AddOpenTelemetry(logging =>
        {
            var res = ResourceBuilder.CreateDefault();
            ConfigureResource(res);
            logging.SetResourceBuilder(res);
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;

            logging.AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(logsEndpointStr);
                opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                if (!string.IsNullOrWhiteSpace(headersStr))
                {
                    opt.Headers = headersStr;
                }
            });
        });

        // 3. OpenTelemetry Tracing & Metrics
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(ConfigureResource)
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                        opts.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value;
                            return path is null || !path.Equals("/health", StringComparison.OrdinalIgnoreCase);
                        };
                        opts.EnrichWithHttpRequest = (activity, request) =>
                        {
                            activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                            if (request.Headers.TryGetValue("User-Agent", out var ua))
                            {
                                activity.SetTag("http.user_agent", ua.ToString());
                            }
                        };
                        opts.EnrichWithHttpResponse = (activity, response) =>
                        {
                            activity.SetTag("http.response_content_length", response.ContentLength);
                        };
                    })
                    .AddHttpClientInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                    })
                    .AddNpgsql()
                    .AddSource(MedMatchActivitySource.Name)
                    .AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(tracesEndpointStr);
                        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                        if (!string.IsNullOrWhiteSpace(headersStr))
                        {
                            opt.Headers = headersStr;
                        }
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(MedMatchMetrics.MeterName)
                    .AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(metricsEndpointStr);
                        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                        if (!string.IsNullOrWhiteSpace(headersStr))
                        {
                            opt.Headers = headersStr;
                        }
                    });
            });

        return builder;
    }
}
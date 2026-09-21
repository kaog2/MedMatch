import { WebTracerProvider, BatchSpanProcessor } from '@opentelemetry/sdk-trace-web';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { resourceFromAttributes } from '@opentelemetry/resources';
import { ATTR_SERVICE_NAME, ATTR_SERVICE_VERSION } from '@opentelemetry/semantic-conventions';
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { trace, context } from '@opentelemetry/api';
import { onCLS, onFCP, onINP, onLCP, onTTFB, type Metric } from 'web-vitals';
import { useAuthStore } from '../store';

// 1. Resolve configuration from runtime window object or build-time Vite env vars
const config = () => {
  const runtime = typeof window !== 'undefined' ? window.__MEDMATCH_CONFIG__ : undefined;
  const env = (import.meta as unknown as { env?: Record<string, string> }).env || {};

  const baseEndpoint = (
    runtime?.OTEL_EXPORTER_OTLP_ENDPOINT ||
    env.VITE_OTEL_EXPORTER_OTLP_ENDPOINT ||
    env.OTEL_EXPORTER_OTLP_ENDPOINT ||
    'https://otel.kevdevs.org'
  ).replace(/\/+$/, '');

  const authToken =
    runtime?.OTEL_AUTH_TOKEN ||
    env.VITE_OTEL_AUTH_TOKEN ||
    env.OTEL_AUTH_TOKEN ||
    '';

  const rawHeaders =
    runtime?.OTEL_EXPORTER_OTLP_HEADERS ||
    env.VITE_OTEL_EXPORTER_OTLP_HEADERS ||
    env.OTEL_EXPORTER_OTLP_HEADERS;

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  };

  if (rawHeaders) {
    for (const part of rawHeaders.split(',')) {
      const [k, ...v] = part.split('=');
      if (k && v.length) headers[k.trim()] = v.join('=').trim();
    }
  } else if (authToken) {
    headers['Authorization'] = `Bearer ${authToken}`;
  }

  const serviceName =
    runtime?.OTEL_SERVICE_NAME ||
    runtime?.OTEL_FRONTEND_SERVICE_NAME ||
    env.VITE_OTEL_SERVICE_NAME ||
    env.OTEL_FRONTEND_SERVICE_NAME ||
    'medmatch-frontend';

  return {
    baseEndpoint,
    logsUrl: `${baseEndpoint}/v1/logs`,
    tracesUrl: `${baseEndpoint}/v1/traces`,
    metricsUrl: `${baseEndpoint}/v1/metrics`,
    headers,
    serviceName,
  };
};

export interface TelemetryLogRecord {
  timestamp: number;
  severity: 'DEBUG' | 'INFO' | 'WARN' | 'ERROR';
  message: string;
  attributes?: Record<string, unknown>;
}

class TelemetryManager {
  private initialized = false;
  private logBuffer: TelemetryLogRecord[] = [];
  private flushTimer: number | null = null;
  private tracerProvider: WebTracerProvider | null = null;

  public init(): void {
    if (this.initialized || typeof window === 'undefined') return;
    this.initialized = true;

    const { tracesUrl, headers, serviceName } = config();

    try {
      // Setup OpenTelemetry Tracing
      const resource = resourceFromAttributes({
        [ATTR_SERVICE_NAME]: serviceName,
        [ATTR_SERVICE_VERSION]: '0.1.0',
        'browser.language': navigator.language,
        'browser.platform': navigator.platform,
      });

      const exporter = new OTLPTraceExporter({
        url: tracesUrl,
        headers,
      });

      const spanProcessor = new BatchSpanProcessor(exporter, {
        maxQueueSize: 100,
        maxExportBatchSize: 10,
        scheduledDelayMillis: 2000,
      });

      this.tracerProvider = new WebTracerProvider({
        resource,
        spanProcessors: [spanProcessor],
      });

      this.tracerProvider.register();

      // Register web instrumentations (Fetch with traceparent header propagation, Document load)
      registerInstrumentations({
        tracerProvider: this.tracerProvider,
        instrumentations: [
          new FetchInstrumentation({
            propagateTraceHeaderCorsUrls: [/.*/],
            clearTimingResources: true,
          }),
          new DocumentLoadInstrumentation(),
        ],
      });
    } catch (err) {
      console.warn('[Telemetry] Tracing setup note:', err);
    }

    // Setup Global Error Listeners
    window.addEventListener('error', (event) => {
      this.log('ERROR', event.message || 'Uncaught window error', {
        filename: event.filename,
        lineno: event.lineno,
        colno: event.colno,
        stack: event.error?.stack,
      });
    });

    window.addEventListener('unhandledrejection', (event) => {
      const reason = event.reason;
      this.log('ERROR', 'Unhandled Promise Rejection', {
        reason: typeof reason === 'object' ? (reason?.message || JSON.stringify(reason)) : String(reason),
        stack: reason?.stack,
      });
    });

    window.addEventListener('beforeunload', () => {
      this.flushLogs(true);
    });

    // Setup Web Vitals Collection
    this.initWebVitals();

    this.log('INFO', 'Frontend telemetry initialized successfully', {
      service: serviceName,
      url: window.location.href,
    });
  }

  private initWebVitals(): void {
    const reportVital = (metric: Metric) => {
      this.log('INFO', `[WebVital] ${metric.name}: ${metric.value.toFixed(2)}`, {
        metric_name: metric.name,
        metric_value: metric.value,
        metric_rating: metric.rating,
        metric_delta: metric.delta,
        metric_id: metric.id,
      });
      this.sendMetric(metric.name, metric.value, { rating: metric.rating });
    };

    try {
      onCLS(reportVital);
      onLCP(reportVital);
      onFCP(reportVital);
      onTTFB(reportVital);
      onINP(reportVital);
    } catch (e) {
      console.warn('[Telemetry] Web vitals initialization note:', e);
    }
  }

  public log(severity: 'DEBUG' | 'INFO' | 'WARN' | 'ERROR', message: string, attributes?: Record<string, unknown>): void {
    const activeSpan = trace.getSpan(context.active());
    const spanContext = activeSpan?.spanContext();

    const { email, roles } = useAuthStore.getState();

    const enrichedAttributes: Record<string, unknown> = {
      'url.path': window.location.pathname,
      'url.full': window.location.href,
      'user_agent': navigator.userAgent,
      ...(email ? { 'user.email': email, 'user.roles': roles?.join(',') } : {}),
      ...(spanContext?.traceId ? { 'trace_id': spanContext.traceId } : {}),
      ...(spanContext?.spanId ? { 'span_id': spanContext.spanId } : {}),
      ...attributes,
    };

    const record: TelemetryLogRecord = {
      timestamp: Date.now(),
      severity,
      message,
      attributes: enrichedAttributes,
    };

    this.logBuffer.push(record);

    if (import.meta.env?.DEV) {
      const style = severity === 'ERROR' ? 'color: red;' : severity === 'WARN' ? 'color: orange;' : 'color: #2196f3;';
      console.log(`%c[${severity}] ${message}`, style, enrichedAttributes);
    }

    if (severity === 'ERROR' || this.logBuffer.length >= 20) {
      this.flushLogs();
    } else if (!this.flushTimer) {
      this.flushTimer = window.setTimeout(() => this.flushLogs(), 4000);
    }
  }

  public async flushLogs(useBeacon = false): Promise<void> {
    if (this.flushTimer) {
      clearTimeout(this.flushTimer);
      this.flushTimer = null;
    }

    if (this.logBuffer.length === 0) return;

    const records = [...this.logBuffer];
    this.logBuffer = [];

    const { logsUrl, headers, serviceName } = config();

    // Convert to standard OTLP JSON Log format
    const otlpPayload = {
      resourceLogs: [
        {
          resource: {
            attributes: [
              { key: 'service.name', value: { stringValue: serviceName } },
              { key: 'service.version', value: { stringValue: '0.1.0' } },
            ],
          },
          scopeLogs: [
            {
              scope: { name: 'medmatch-web-logger' },
              logRecords: records.map((r) => ({
                timeUnixNano: `${r.timestamp}000000`,
                severityText: r.severity,
                body: { stringValue: r.message },
                attributes: Object.entries(r.attributes || {}).map(([k, v]) => ({
                  key: k,
                  value: { stringValue: typeof v === 'object' ? JSON.stringify(v) : String(v) },
                })),
              })),
            },
          ],
        },
      ],
    };

    const body = JSON.stringify(otlpPayload);

    if (useBeacon && navigator.sendBeacon) {
      try {
        const blob = new Blob([body], { type: 'application/json' });
        navigator.sendBeacon(logsUrl, blob);
        return;
      } catch {
        // Fall back to fetch below
      }
    }

    try {
      await fetch(logsUrl, {
        method: 'POST',
        headers,
        body,
        keepalive: true,
      });
    } catch {
      // Silently ignore network export failures to never interfere with client UI
    }
  }

  public async sendMetric(name: string, value: number, attributes?: Record<string, unknown>): Promise<void> {
    const { metricsUrl, headers, serviceName } = config();

    const otlpMetricsPayload = {
      resourceMetrics: [
        {
          resource: {
            attributes: [
              { key: 'service.name', value: { stringValue: serviceName } },
            ],
          },
          scopeMetrics: [
            {
              scope: { name: 'medmatch-web-metrics' },
              metrics: [
                {
                  name,
                  gauge: {
                    dataPoints: [
                      {
                        timeUnixNano: `${Date.now()}000000`,
                        asDouble: value,
                        attributes: Object.entries(attributes || {}).map(([k, v]) => ({
                          key: k,
                          value: { stringValue: String(v) },
                        })),
                      },
                    ],
                  },
                },
              ],
            },
          ],
        },
      ],
    };

    try {
      await fetch(metricsUrl, {
        method: 'POST',
        headers,
        body: JSON.stringify(otlpMetricsPayload),
        keepalive: true,
      });
    } catch {
      // Silently ignore metrics export failures
    }
  }
}

export const telemetry = new TelemetryManager();

export const logger = {
  debug: (msg: string, attrs?: Record<string, unknown>) => telemetry.log('DEBUG', msg, attrs),
  info: (msg: string, attrs?: Record<string, unknown>) => telemetry.log('INFO', msg, attrs),
  warn: (msg: string, attrs?: Record<string, unknown>) => telemetry.log('WARN', msg, attrs),
  error: (msg: string, error?: unknown, attrs?: Record<string, unknown>) => {
    const errAttrs: Record<string, unknown> = { ...attrs };
    if (error instanceof Error) {
      errAttrs.error_name = error.name;
      errAttrs.error_message = error.message;
      errAttrs.error_stack = error.stack;
    } else if (error) {
      errAttrs.error = String(error);
    }
    telemetry.log('ERROR', msg, errAttrs);
  },
};

export function initTelemetry(): void {
  telemetry.init();
}

export function recordPageView(path: string): void {
  logger.info(`Navigated to ${path}`, { 'navigation.path': path });
}


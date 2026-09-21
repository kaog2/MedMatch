/// <reference types="vite/client" />

interface Window {
  __MEDMATCH_CONFIG__?: {
    GOOGLE_CLIENT_ID?: string;
    API_URL?: string;
    OTEL_EXPORTER_OTLP_ENDPOINT?: string;
    OTEL_AUTH_TOKEN?: string;
    OTEL_EXPORTER_OTLP_HEADERS?: string;
    OTEL_SERVICE_NAME?: string;
    OTEL_FRONTEND_SERVICE_NAME?: string;
  };
}

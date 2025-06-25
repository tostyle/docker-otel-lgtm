const dotenv = require('dotenv')
const { NodeSDK } = require('@opentelemetry/sdk-node')
const { getNodeAutoInstrumentations } = require('@opentelemetry/auto-instrumentations-node')
const { PeriodicExportingMetricReader } = require('@opentelemetry/sdk-metrics')
const { OTLPTraceExporter } = require('@opentelemetry/exporter-trace-otlp-proto')
const { OTLPMetricExporter } = require('@opentelemetry/exporter-metrics-otlp-proto')
const { diag, DiagConsoleLogger, DiagLogLevel } = require('@opentelemetry/api')

dotenv.config()

const DT_ENVIRONMENT_ID = process.env.DT_ENVIRONMENT_ID || ''
const DT_API_TOKEN = process.env.DT_API_TOKEN || ''

const httpTraceExporterOptions = {
  // url: "http://localhost:4334/v1/traces", // http
  url: `https://${DT_ENVIRONMENT_ID}.live.dynatrace.com/api/v2/otlp/v1/traces`,
  headers: {
    // "Content-Type": "application/json",
    'Content-Type': 'application/x-protobuf',
    Authorization: `Api-Token ${DT_API_TOKEN}`
  }
}

const httpMetricExporterOptions = {
  // url: "http://localhost:4334/v1/traces", // http
  url: `https://${DT_ENVIRONMENT_ID}.live.dynatrace.com/api/v2/otlp/v1/metrics`,
  headers: {
    // "Content-Type": "application/json",
    // 'Content-Type': 'application/x-protobuf',
    Authorization: `Api-Token ${DT_API_TOKEN}`
  }
}

diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.DEBUG)
const sdk = new NodeSDK({
  traceExporter: new OTLPTraceExporter(httpTraceExporterOptions),
  metricReader: new PeriodicExportingMetricReader({
    exporter: new OTLPMetricExporter(httpMetricExporterOptions)
  }),
  instrumentations: [
    getNodeAutoInstrumentations({
      '@opentelemetry/instrumentation-http': {
        ignoreIncomingRequestHook: (request) => {
          if (request.url === '/favicon.ico') {
            return true
          }
          return false
        }
      }
    })
  ]
})

sdk.start()


# LGTM Observability Demo Workflow

This guide walks you through running the full LGTM (Loki, Grafana, Tempo, Mimir) stack with OpenTelemetry Collector, running a demo application, generating traffic, and viewing metrics in Grafana.

---

# Option 1

## 1. Run docker compose to start LGTM stack and OpenTelemetry Collector

```sh
docker compose up -d
```

Or start each statck separately

# Option 2

## 1. Start the LGTM Stack

Run the main LGTM stack using Docker Compose:

```sh
docker compose -f docker-compose-otel.yaml up -d
```

This will start the LGTM stack (Grafana, Loki, Prometheus, etc.) in the background.

---

## 2. Start the OpenTelemetry Collector

In a separate terminal, start the OpenTelemetry Collector:

```sh
docker compose -f otel-collector.docker-compose.yaml up -d
```

This will start the collector service required for ingesting telemetry data.

---

## 3. Run a Demo Application

Pick one of the example folders (e.g., `examples/go`, `examples/nodejs`, `examples/python`, etc.).

For example, to run the Go demo:

```sh
cd examples/go
./run.sh
```

Or for Node.js:

```sh
cd examples/nodejs
./run.sh
```

---

## 4. Generate Traffic

Use `curl` to generate traffic to the demo application (replace the port if your example uses a different one):

```sh
curl http://127.0.0.1:8083/rolldice
```

Repeat as needed to generate more telemetry data.

---

## 5. View Metrics in Grafana

1. Open your browser and go to: [http://localhost:3000](http://localhost:3000)
2. Log in with the default credentials (often `admin`/`admin` or as specified in your setup).
3. Explore dashboards to view metrics, logs, and traces from your demo application.

---

## Notes
- Make sure all containers are running (`docker ps`).
- You can stop all services with:
  ```sh
  docker compose -f docker-compose-otel.yaml down
  docker compose -f otel-collector.docker-compose.yaml down
  ```
- For troubleshooting, check container logs:
  ```sh
  docker compose -f docker-compose-otel.yaml logs
  docker compose -f otel-collector.docker-compose.yaml logs
  ```

---

Enjoy exploring observability with LGTM and OpenTelemetry!

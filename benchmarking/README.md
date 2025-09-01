# Benchmarking and Load Tests

This directory contains load-testing scripts for the backend using both Artillery and k6.

## Artillery Benchmark Tests

These tests complement the k6 load scripts by using [Artillery](https://artillery.io) to benchmark core endpoints.

### Prerequisites
- Node.js
- Access to the backend API at `http://localhost:5000`
- InfluxDB token exported as `INFLUX_TOKEN`

Install Artillery and the InfluxDB plugin:

```bash
npm install -g artillery @artilleryio/influxdb
```

### Running the test

```bash
npx artillery run --config config.js auth.yml
```

Metrics will be sent to the `k6` bucket in InfluxDB and can be viewed in Grafana.

## k6 Load Tests

All scripts stream metrics to InfluxDB v2 when executed with `--out influxdb=http://localhost:8086/k6?org=battletanks&token=k6-token`.

### Authentication
Simulates 100 users registering and logging in simultaneously, tracking 4xx/5xx errors.

```bash
API_BASE_URL=http://localhost:5000 \
k6 run --out influxdb=http://localhost:8086/k6?org=battletanks&token=k6-token benchmarking/auth.js
```

### SignalR Movement
Simulates 20 players sending position updates in real time.

```bash
SIGNALR_URL=ws://localhost:5000/game-hub ROOM_CODE=test \
k6 run --out influxdb=http://localhost:8086/k6?org=battletanks&token=k6-token benchmarking/signalr.js
```

### MQTT Power-up Notifications
Subscribes 50 clients to `powerups/#` and counts received messages.

```bash
MQTT_URL=mqtt://localhost:1883 \
k6 run --out influxdb=http://localhost:8086/k6?org=battletanks&token=k6-token benchmarking/mqtt.js
```

Set the environment variables to point to the correct backend services if they are running elsewhere.

# k6 Load Tests

Scripts for exercising the backend under load. All tests can push metrics to InfluxDB v2 by running k6 with `--out influxdb=http://localhost:8086/k6?org=battletanks&token=k6-token`.

## Authentication
Simulates 100 users registering and logging in simultaneously, tracking 4xx/5xx errors.

```bash
API_BASE_URL=http://localhost:5000 \
k6 run --out influxdb=http://localhost:8086/k6?org=battletanks&token=k6-token auth.js
```

## SignalR Movement
Simulates 20 players sending position updates in real time.

```bash
SIGNALR_URL=ws://localhost:5000/game-hub ROOM_CODE=test \
k6 run --out influxdb=http://localhost:8086/k6?org=battletanks&token=k6-token signalr.js
```

## MQTT Power-up Notifications
Subscribes 50 clients to `powerups/#` and counts received messages.

```bash
MQTT_URL=mqtt://localhost:1883 \
k6 run --out influxdb=http://localhost:8086/k6?org=battletanks&token=k6-token mqtt.js
```

Set the environment variables to point to the correct backend services if they are running elsewhere.

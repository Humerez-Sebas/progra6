# Artillery Benchmark Tests

These tests complement the k6 load scripts by using [Artillery](https://artillery.io) to benchmark core endpoints.

## Prerequisites
- Node.js
- Access to the backend API at `http://localhost:5000`
- InfluxDB token exported as `INFLUX_TOKEN`

Install Artillery and the InfluxDB plugin:

```bash
npm install -g artillery @artilleryio/influxdb
```

## Running the test

Execute with the separate config and scenario files:

```bash
npx artillery run --config config.js auth.yml
```

Metrics will be sent to the `k6` bucket in InfluxDB and can be viewed in Grafana.

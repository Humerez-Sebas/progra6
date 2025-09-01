# k6 Load Tests

Simple k6 script to exercise the authentication endpoints.

## Running

```bash
k6 run --out influxdb=http://localhost:8086/k6 auth.js
```

Set `API_BASE_URL` environment variable if backend is running elsewhere.

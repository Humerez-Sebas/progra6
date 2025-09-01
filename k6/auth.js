import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter } from 'k6/metrics';

export const options = {
  vus: 100,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_4xx: ['count==0'],
    http_5xx: ['count==0'],
  },
};

const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:5000';
const clientErrors = new Counter('http_4xx');
const serverErrors = new Counter('http_5xx');

export default function () {
  const username = `user_${Math.random().toString(36).substring(2, 8)}`;
  const params = { headers: { 'Content-Type': 'application/json' } };

  const registerPayload = JSON.stringify({
    username,
    email: `${username}@example.com`,
    password: 'Password123!',
    confirmPassword: 'Password123!',
  });

  const registerRes = http.post(`${BASE_URL}/auth/register`, registerPayload, params);
  check(registerRes, { 'register 200': (r) => r.status === 200 });
  if (registerRes.status >= 400 && registerRes.status < 500) clientErrors.add(1);
  if (registerRes.status >= 500) serverErrors.add(1);

  const loginPayload = JSON.stringify({
    usernameOrEmail: username,
    password: 'Password123!',
  });

  const loginRes = http.post(`${BASE_URL}/auth/login`, loginPayload, params);
  check(loginRes, { 'login 200': (r) => r.status === 200 });
  if (loginRes.status >= 400 && loginRes.status < 500) clientErrors.add(1);
  if (loginRes.status >= 500) serverErrors.add(1);

  sleep(1);
}

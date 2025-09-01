import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 1,
  iterations: 1,
};

const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:5000';

export default function () {
  const username = `user_${Math.random().toString(36).substring(2,8)}`;
  const params = { headers: { 'Content-Type': 'application/json' } };

  const registerPayload = JSON.stringify({
    username,
    email: `${username}@example.com`,
    password: 'Password123!',
    confirmPassword: 'Password123!'
  });

  const registerRes = http.post(`${BASE_URL}/auth/register`, registerPayload, params);
  check(registerRes, { 'register 200': (r) => r.status === 200 });

  const loginPayload = JSON.stringify({
    usernameOrEmail: username,
    password: 'Password123!'
  });

  const loginRes = http.post(`${BASE_URL}/auth/login`, loginPayload, params);
  check(loginRes, { 'login 200': (r) => r.status === 200 });

  sleep(1);
}

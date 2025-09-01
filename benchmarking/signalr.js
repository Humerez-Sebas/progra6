import signalr from 'k6/x/signalr';
import { Trend } from 'k6/metrics';

export const options = {
  vus: 20,
  duration: '1m',
};

const BASE_URL = __ENV.SIGNALR_URL || 'ws://localhost:5000/game-hub';
const ROOM_CODE = __ENV.ROOM_CODE || 'test';
const latency = new Trend('signalr_latency');

export default function () {
  const client = new signalr.Client();
  client.start(BASE_URL, { transport: signalr.TransportType.WebSockets });

  client.invoke('JoinRoom', ROOM_CODE, `user_${__VU}`);

  for (let i = 0; i < 20; i++) {
    const start = Date.now();
    client.invoke('UpdatePosition', {
      x: Math.random() * 100,
      y: Math.random() * 100,
      rotation: 0,
      timestamp: Date.now(),
    });
    latency.add(Date.now() - start);
  }

  client.stop();
}

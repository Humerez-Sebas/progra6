import mqtt from 'k6/x/mqtt';
import { Counter } from 'k6/metrics';
import { sleep } from 'k6';

export const options = {
  vus: 50,
  duration: '1m',
};

const BROKER_URL = __ENV.MQTT_URL || 'mqtt://localhost:1883';
const topic = 'powerups/#';
const messages = new Counter('mqtt_messages');

export default function () {
  const client = mqtt.connect(BROKER_URL, { clientId: `k6-${__VU}` });
  client.subscribe(topic);
  client.on('message', function () {
    messages.add(1);
  });

  sleep(10);
  client.disconnect();
}

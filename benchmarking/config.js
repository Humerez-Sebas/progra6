module.exports = {
  target: process.env.API_BASE_URL || 'http://localhost:5000',
  phases: [
    { duration: 30, arrivalRate: 10 }
  ],
  plugins: {
    '@artilleryio/influxdb': {
      host: process.env.INFLUX_URL || 'http://localhost:8086',
      token: process.env.INFLUX_TOKEN || 'your-token',
      org: process.env.INFLUX_ORG || 'primary',
      bucket: process.env.INFLUX_BUCKET || 'k6',
      measurement: 'artillery'
    }
  },
  processor: {
    randomUser(context, events, done) {
      const username = 'user_' + Math.random().toString(36).substring(2, 8);
      context.vars.username = username;
      context.vars.email = `${username}@example.com`;
      return done();
    }
  }
};

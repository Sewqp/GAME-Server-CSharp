const { Pool } = require('pg');
const Redis = require('ioredis');

const pgPool = new Pool({
  host: process.env.DB_HOST,
  port: Number(process.env.DB_PORT),
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_NAME,
  max: 10,
});

const redis = new Redis(process.env.REDIS_URL, {
  lazyConnect: false,
  retryStrategy: (times) => Math.min(times * 200, 2000),
});

redis.on('error', (err) => {
  console.error(`[Redis] connection error: ${err.message}`);
});

module.exports = { pgPool, redis };

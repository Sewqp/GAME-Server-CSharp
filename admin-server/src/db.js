const mysql = require('mysql2/promise');
const Redis = require('ioredis');

const mysqlPool = mysql.createPool({
  host: process.env.DB_HOST,
  port: Number(process.env.DB_PORT),
  user: process.env.DB_USER,
  password: process.env.DB_PASSWORD,
  database: process.env.DB_NAME,
  waitForConnections: true,
  connectionLimit: 10,
});

const redis = new Redis(process.env.REDIS_URL, {
  lazyConnect: false,
  retryStrategy: (times) => Math.min(times * 200, 2000),
});

redis.on('error', (err) => {
  console.error(`[Redis] connection error: ${err.message}`);
});

module.exports = { mysqlPool, redis };

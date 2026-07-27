const express = require('express');
const { redis } = require('../db');

const SESSION_COUNT_KEY = 'stats:session_count';

const router = express.Router();

router.get('/admin/status', async (req, res) => {
  const redisConnected = redis.status === 'ready';

  let activeSessions = null;
  if (redisConnected) {
    const raw = await redis.get(SESSION_COUNT_KEY);
    activeSessions = raw !== null ? Number(raw) : null;
  }

  res.json({
    redisConnected,
    activeSessions,
    checkedAt: new Date().toISOString(),
  });
});

module.exports = router;

const express = require('express');
const { mysqlPool } = require('../db');

const RANKING_LIMIT = 100;

const router = express.Router();

router.get('/ranking', async (req, res, next) => {
  try {
    const [rows] = await mysqlPool.query(
      `SELECT p.pname, cs.level, cs.hp_max, cs.mp_max
       FROM character_stat cs
       JOIN player p ON p.player_id = cs.player_id
       ORDER BY cs.level DESC
       LIMIT ?`,
      [RANKING_LIMIT]
    );

    res.json(rows);
  } catch (err) {
    next(err);
  }
});

module.exports = router;

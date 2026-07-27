require('dotenv').config();

const express = require('express');
const { createLogger } = require('./logger');
const statusRouter = require('./routes/status');
const rankingRouter = require('./routes/ranking');

const logger = createLogger();
const app = express();

app.use('/api', statusRouter);
app.use('/api', rankingRouter);

// eslint-disable-next-line no-unused-vars
app.use((err, req, res, next) => {
  logger.error(`${req.method} ${req.originalUrl} - ${err.stack ?? err.message}`);
  res.status(500).json({ error: 'internal_server_error' });
});

const port = Number(process.env.PORT) || 4000;
app.listen(port, () => {
  logger.info(`Admin server listening on port ${port}`);
});

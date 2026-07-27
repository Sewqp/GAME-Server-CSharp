const path = require('path');
const fs = require('fs');
const winston = require('winston');
const Transport = require('winston-transport');

const LOG_DIR = path.join(__dirname, '..', 'logs');
const LLM_COOLDOWN_MS = 60_000; // 에러 폭발 시 LLM 과호출 방지

function truncate(text, maxLength) {
  return text.length <= maxLength ? text : `${text.slice(0, maxLength)}...`;
}

class LlmErrorTransport extends Transport {
  constructor(opts) {
    super(opts);
    this.lmStudioUrl = opts.lmStudioUrl;
    this.discordWebhookUrl = opts.discordWebhookUrl;
    this.lastCallAt = 0;
  }

  log(info, callback) {
    setImmediate(() => this.emit('logged', info));

    const now = Date.now();
    if (info.level === 'error' && now - this.lastCallAt >= LLM_COOLDOWN_MS) {
      this.lastCallAt = now;
      this.analyzeAndNotify(info.message).catch((err) => {
        console.error(`[LlmErrorTransport] pipeline failed: ${err.message}`);
      });
    }

    callback();
  }

  async analyzeAndNotify(errorMessage) {
    const analysis = await this.requestLlmAnalysis(errorMessage);
    if (process.env.DEBUG_LLM_PIPELINE) console.log(`[DEBUG] LLM analysis: ${analysis}`);
    if (analysis) await this.postToDiscord(errorMessage, analysis);
  }

  async requestLlmAnalysis(errorMessage) {
    const response = await fetch(this.lmStudioUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        model: 'local-model',
        messages: [
          {
            role: 'user',
            content: `다음 관리 서버 에러 로그를 한국어로 간단히 분석해줘 (원인 추정 + 대응 방안 1~2줄):\n${errorMessage}`,
          },
        ],
        temperature: 0.3,
      }),
    });

    if (!response.ok) return null;
    const data = await response.json();
    return data?.choices?.[0]?.message?.content ?? null;
  }

  async postToDiscord(errorMessage, analysis) {
    if (!this.discordWebhookUrl) return;

    await fetch(this.discordWebhookUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        embeds: [
          {
            title: '관리 서버 에러 감지',
            color: 0xe74c3c,
            fields: [
              { name: '에러', value: truncate(errorMessage, 1000) },
              { name: 'LLM 분석', value: truncate(analysis, 1000) },
            ],
            timestamp: new Date().toISOString(),
          },
        ],
      }),
    });
  }
}

function createLogger() {
  fs.mkdirSync(LOG_DIR, { recursive: true });

  return winston.createLogger({
    level: 'info',
    format: winston.format.combine(
      winston.format.timestamp(),
      winston.format.printf(({ timestamp, level, message }) => `[${timestamp}] [${level}] ${message}`)
    ),
    transports: [
      new winston.transports.Console(),
      new winston.transports.File({ filename: path.join(LOG_DIR, 'admin-server.log') }),
      new LlmErrorTransport({
        lmStudioUrl: process.env.LM_STUDIO_URL,
        discordWebhookUrl: process.env.DISCORD_WEBHOOK_URL,
      }),
    ],
  });
}

module.exports = { createLogger };

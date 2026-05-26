'use strict';

require('dotenv').config();

function requireEnv(name) {
  const value = process.env[name];
  if (!value || !value.trim()) {
    throw new Error(
      `Missing required environment variable: ${name}. ` +
        `Copy .env.example to .env and fill it in.`
    );
  }
  return value.trim();
}

const host = requireEnv('HOST').replace(/\/+$/, '');
const clientId = requireEnv('CLIENT_ID');
const clientSecret = requireEnv('CLIENT_SECRET');
const redirectUri = (process.env.REDIRECT_URI || 'https://webhook.site/your-unique-id-here').trim();
const port = Number(process.env.PORT || 3000);

module.exports = {
  host,
  clientId,
  clientSecret,
  redirectUri,
  port,
  authorizeUrl: `${host}/api/connect/authorize`,
  tokenUrl: `${host}/api/connect/token`,
};

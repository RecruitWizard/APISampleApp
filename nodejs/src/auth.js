'use strict';

const crypto = require('crypto');
const config = require('./config');

/**
 * Build the authorization URL the user's browser should be redirected to.
 *
 * Step 1 of the flow:
 *   GET {HOST}/api/connect/authorize
 *     ?response_type=code
 *     &client_id=...
 *     &redirect_uri=...
 *     &state=...
 */
function buildAuthorizeUrl(state) {
  const params = new URLSearchParams({
    response_type: 'code',
    client_id: config.clientId,
    redirect_uri: config.redirectUri,
    state,
  });
  return `${config.authorizeUrl}?${params.toString()}`;
}

/**
 * Step 3: exchange a one-time authorization code for an access token + refresh token.
 *
 * POST {HOST}/api/connect/token
 * Content-Type: application/x-www-form-urlencoded
 *   client_id, client_secret, grant_type=authorization_code, code
 */
async function exchangeCodeForToken(code) {
  const body = new URLSearchParams({
    client_id: config.clientId,
    client_secret: config.clientSecret,
    grant_type: 'authorization_code',
    code,
  });
  return postToken(body);
}

/**
 * Step 4: exchange a refresh token for a new access token.
 *
 * POST {HOST}/api/connect/token
 * Content-Type: application/x-www-form-urlencoded
 *   client_id, client_secret, grant_type=refresh_token, refresh_token
 */
async function refreshAccessToken(refreshToken) {
  const body = new URLSearchParams({
    client_id: config.clientId,
    client_secret: config.clientSecret,
    grant_type: 'refresh_token',
    refresh_token: refreshToken,
  });
  return postToken(body);
}

async function postToken(body) {
  const res = await fetch(config.tokenUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/x-www-form-urlencoded',
      Accept: 'application/json',
    },
    body,
  });

  const text = await res.text();
  let payload;
  try {
    payload = text ? JSON.parse(text) : {};
  } catch {
    payload = { raw: text };
  }

  if (!res.ok) {
    const err = new Error(
      `Token endpoint returned ${res.status} ${res.statusText}: ${text || '(empty body)'}`
    );
    err.status = res.status;
    err.payload = payload;
    throw err;
  }

  return payload;
}

/**
 * Decode the JWT access token payload (without verifying the signature).
 * Used purely for display + extracting the UserID claim for demo calls.
 */
function decodeJwt(accessToken) {
  if (!accessToken || typeof accessToken !== 'string') return null;
  const parts = accessToken.split('.');
  if (parts.length !== 3) return null;
  try {
    const json = Buffer.from(parts[1], 'base64').toString('utf8');
    return JSON.parse(json);
  } catch {
    return null;
  }
}

function generateState() {
  return crypto.randomBytes(16).toString('hex');
}

module.exports = {
  buildAuthorizeUrl,
  exchangeCodeForToken,
  refreshAccessToken,
  decodeJwt,
  generateState,
};

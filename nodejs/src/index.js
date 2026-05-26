'use strict';

const path = require('path');
const express = require('express');

const config = require('./config');
const auth = require('./auth');
const store = require('./tokenStore');

const app = express();

app.set('view engine', 'ejs');
app.set('views', path.join(__dirname, '..', 'views'));
app.use(express.urlencoded({ extended: false }));
app.use('/assets', express.static(path.join(__dirname, 'assets'), { maxAge: '1h' }));

const pendingStates = new Map();
const STATE_TTL_MS = 10 * 60 * 1000;

function rememberState(state) {
  const expiresAt = Date.now() + STATE_TTL_MS;
  pendingStates.set(state, expiresAt);
  for (const [s, exp] of pendingStates) {
    if (exp < Date.now()) pendingStates.delete(s);
  }
}

function consumeState(state) {
  if (!state) return false;
  const expiresAt = pendingStates.get(state);
  if (!expiresAt) return false;
  pendingStates.delete(state);
  return expiresAt >= Date.now();
}

app.get('/', (req, res) => {
  const tokens = store.getTokens();
  res.render('index', {
    host: config.host,
    redirectUri: config.redirectUri,
    clientId: config.clientId,
    tokens,
    jwtClaims: tokens ? auth.decodeJwt(tokens.access_token) : null,
    lastApiResult: store.getLastApiResult(),
    lastTokenResponse: store.getLastTokenResponse(),
    prefillCode: req.query.code || '',
    message: req.query.message || null,
    error: req.query.error || null,
  });
});

app.get('/auth/login', (req, res) => {
  const state = auth.generateState();
  rememberState(state);
  const url = auth.buildAuthorizeUrl(state);
  console.log(`[auth] Redirecting to authorize URL with state=${state}`);
  res.redirect(url);
});

app.get('/auth/callback', (req, res) => {
  const { code, state, error, error_description: errorDescription } = req.query;

  if (error) {
    const detail = errorDescription ? `${error}: ${errorDescription}` : String(error);
    return res.status(400).render('callback', {
      ok: false,
      message: `Authorization denied (${detail})`,
      code: null,
      state: state || null,
    });
  }

  if (!code) {
    return res.status(400).render('callback', {
      ok: false,
      message: 'Callback hit without a code parameter.',
      code: null,
      state: state || null,
    });
  }

  if (!consumeState(state)) {
    return res.status(400).render('callback', {
      ok: false,
      message: 'State mismatch or expired. Please start the login again from the main tab.',
      code: String(code),
      state: state || null,
    });
  }

  console.log(`[auth] Received code (${String(code).slice(0, 8)}...) – waiting for manual exchange.`);
  res.render('callback', {
    ok: true,
    message: null,
    code: String(code),
    state: state || null,
  });
});

app.post('/auth/exchange', async (req, res) => {
  const code = (req.body && req.body.code ? String(req.body.code) : '').trim();
  if (!code) {
    return res.redirect(`/?error=${encodeURIComponent('No code supplied to exchange.')}`);
  }

  try {
    console.log(`[auth] Exchanging code for token (code=${code.slice(0, 8)}...)`);
    const payload = await auth.exchangeCodeForToken(code);
    store.setTokens(payload);
    res.redirect(`/?message=${encodeURIComponent('Access token retrieved successfully.')}`);
  } catch (err) {
    console.error('[auth] Token exchange failed:', err);
    res.redirect(`/?error=${encodeURIComponent(err.message)}&code=${encodeURIComponent(code)}`);
  }
});

app.post('/auth/refresh', async (req, res) => {
  const tokens = store.getTokens();
  if (!tokens || !tokens.refresh_token) {
    return res.redirect(`/?error=${encodeURIComponent('No refresh token available.')}`);
  }
  try {
    console.log('[auth] Refreshing access token...');
    const payload = await auth.refreshAccessToken(tokens.refresh_token);
    store.setTokens(payload);
    res.redirect(`/?message=${encodeURIComponent('Access token refreshed.')}`);
  } catch (err) {
    console.error('[auth] Refresh failed:', err);
    res.redirect(`/?error=${encodeURIComponent(err.message)}`);
  }
});

app.post('/auth/logout', (req, res) => {
  store.clearTokens();
  res.redirect(`/?message=${encodeURIComponent('Tokens cleared.')}`);
});

app.post('/api/server-status', async (req, res) => {
  await callApi(res, 'GET', '/api/ServerStatus');
});

app.post('/api/me', async (req, res) => {
  const tokens = store.getTokens();
  if (!tokens) {
    return res.redirect(`/?error=${encodeURIComponent('Not authenticated.')}`);
  }
  const claims = auth.decodeJwt(tokens.access_token);
  const userId = claims && (claims.UserID || claims.userId || claims.sub);
  if (!userId) {
    return res.redirect(
      `/?error=${encodeURIComponent('Could not find UserID claim on the access token.')}`
    );
  }
  await callApi(res, 'GET', `/api/Users/${encodeURIComponent(userId)}`);
});

async function callApi(res, method, apiPath) {
  const tokens = store.getTokens();
  if (!tokens) {
    return res.redirect(`/?error=${encodeURIComponent('Not authenticated.')}`);
  }
  const url = `${config.host}${apiPath}`;
  console.log(`[api] ${method} ${url}`);
  try {
    const apiRes = await fetch(url, {
      method,
      headers: {
        Authorization: `Bearer ${tokens.access_token}`,
        Accept: 'application/json',
      },
    });
    const text = await apiRes.text();
    let body;
    try {
      body = text ? JSON.parse(text) : null;
    } catch {
      body = text;
    }
    store.setLastApiResult({
      method,
      url,
      status: apiRes.status,
      statusText: apiRes.statusText,
      body,
      at: new Date().toISOString(),
    });
    res.redirect('/');
  } catch (err) {
    console.error('[api] Request failed:', err);
    res.redirect(`/?error=${encodeURIComponent(err.message)}`);
  }
}

app.listen(config.port, () => {
  console.log(`\nRecruit Wizard auth sample running at http://localhost:${config.port}`);
  console.log(`  HOST          = ${config.host}`);
  console.log(`  CLIENT_ID     = ${config.clientId}`);
  console.log(`  REDIRECT_URI  = ${config.redirectUri}`);
  console.log(`  authorize URL = ${config.authorizeUrl}`);
  console.log(`  token URL     = ${config.tokenUrl}\n`);
});

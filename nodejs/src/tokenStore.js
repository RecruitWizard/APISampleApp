'use strict';

/**
 * Very simple in-memory token store. Single user, single process.
 * In a real app, persist tokens per-user (encrypted) in a database.
 */
let tokens = null;
let lastApiResult = null;
let lastTokenResponse = null;

function setTokens(payload) {
  tokens = {
    access_token: payload.access_token,
    access_token_expiry: payload.access_token_expiry,
    refresh_token: payload.refresh_token,
    refresh_token_expiry: payload.refresh_token_expiry,
    received_at: new Date().toISOString(),
  };
  lastTokenResponse = {
    payload,
    at: tokens.received_at,
  };
  return tokens;
}

function getTokens() {
  return tokens;
}

function clearTokens() {
  tokens = null;
  lastApiResult = null;
  lastTokenResponse = null;
}

function setLastApiResult(result) {
  lastApiResult = result;
}

function getLastApiResult() {
  return lastApiResult;
}

function getLastTokenResponse() {
  return lastTokenResponse;
}

module.exports = {
  setTokens,
  getTokens,
  clearTokens,
  setLastApiResult,
  getLastApiResult,
  getLastTokenResponse,
};

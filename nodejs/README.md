# Recruit Wizard API – Auth Flow Sample (Node.js)

A small Express app that demonstrates the **OAuth 2.0 authorization code flow**
used by the [Recruit Wizard API](https://sandbox.recruitwizard.site/api-docs/#description/authentication).

> This is one of several language samples in this repository. See the
> [top-level README](../README.md) for the full list.

## What it shows

1. Redirect the user to `GET {HOST}/api/connect/authorize` (opened in a new tab)
2. Recruit Wizard redirects back to the registered callback URL, where the
   sample displays the authorization `code` on a copy-friendly page
3. Paste the code into the input on the main tab and exchange it for tokens
   via `POST {HOST}/api/connect/token` (`grant_type=authorization_code`)
4. Refresh access tokens with `grant_type=refresh_token`
5. Call protected endpoints (`/api/ServerStatus`, `/api/Users/{id}`) using
   the access token in an `Authorization: Bearer …` header

## Prerequisites

- Node.js 18+ (uses built-in `fetch`)
- A Recruit Wizard API client (`client_id` + `client_secret`) with a registered
  redirect URI matching `REDIRECT_URI` below (default
  `http://localhost:3000/auth/callback`)

## Setup

From this folder (`nodejs/`):

```bash
npm install
cp .env.example .env          # Windows: copy .env.example .env
# then edit .env with your values
npm start
```

Then open <http://localhost:3000>.

## Environment variables

| Variable        | Description                                                                       |
| --------------- | --------------------------------------------------------------------------------- |
| `HOST`          | Base URL of the Recruit Wizard API (e.g. `https://sandbox.recruitwizard.site`)    |
| `CLIENT_ID`     | OAuth client ID issued by Recruit Wizard                                          |
| `CLIENT_SECRET` | OAuth client secret issued by Recruit Wizard                                      |
| `REDIRECT_URI`  | Callback URL — must exactly match one of the URLs registered with your client     |
| `PORT`          | Local port to listen on (optional, default `3000`)                                |

## Routes

| Path                  | Method | What it does                                                                  |
| --------------------- | ------ | ----------------------------------------------------------------------------- |
| `/`                   | GET    | Home page — current token state, paste-code input, action buttons             |
| `/auth/login`         | GET    | Generates a `state`, redirects to the authorize endpoint                      |
| `/auth/callback`      | GET    | Lands here from Recruit Wizard; shows the `code` with a Copy button           |
| `/auth/exchange`      | POST   | Exchanges the pasted `code` for an access + refresh token                     |
| `/auth/refresh`       | POST   | Uses the stored refresh token to mint a new access token                      |
| `/auth/logout`        | POST   | Clears the in-memory tokens                                                   |
| `/api/server-status`  | POST   | Calls `GET /api/ServerStatus` using the access token                          |
| `/api/me`             | POST   | Decodes the JWT, then calls `GET /api/Users/{UserID}`                         |
| `/assets/*`           | GET    | Static files served from `src/assets/` (logo, etc.)                           |

## Project layout

```
nodejs/
├── package.json
├── .env.example
├── src/
│   ├── index.js         # Express server + routes
│   ├── config.js        # Loads + validates env vars
│   ├── auth.js          # Authorize URL, token + refresh calls, JWT decode
│   ├── tokenStore.js    # In-memory token storage
│   └── assets/
│       └── full-logo-dark.png
└── views/
    ├── index.ejs        # Home page (paste-code UI)
    └── callback.ejs     # Page shown after Recruit Wizard redirects back
```

## Notes

- Tokens are kept **in memory only**. Restart the process and you start fresh.
  In a real application, persist them per-user in a database (encrypted at rest).
- The `state` parameter is generated per login attempt and validated on the
  callback to mitigate CSRF.
- The code is intentionally framework-light so it's easy to lift into your own app.

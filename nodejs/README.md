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
- A Recruit Wizard API client (`client_id` + `client_secret`) with a redirect
  URI that Recruit Wizard support has white-listed on your client. The sample
  defaults to a `https://webhook.site/<uuid>` placeholder so you don't need
  to expose localhost via ngrok or a similar tunnel — see
  ["Testing with webhook.site"](#testing-with-webhooksite-no-localhost-callback-needed)
  below.

> **Heads-up:** redirect URIs are white-listed per client by Recruit Wizard.
> If the URL you want to use isn't already registered, send it to Recruit
> Wizard support and ask them to add it to your client — otherwise the
> authorize step will fail with a `redirect_uri` mismatch.

## Setup

From this folder (`nodejs/`):

```bash
npm install
cp .env.example .env          # Windows: copy .env.example .env
# then edit .env with your values
npm start
```

Then open <http://localhost:3000>.

## Testing with webhook.site (no localhost callback needed)

Recruit Wizard typically white-lists HTTPS redirect URIs only, so a plain
`http://localhost:…/auth/callback` won't work without exposing your machine
through ngrok or a similar tunnel. The simplest workaround — and the default
this sample ships with — is to use [webhook.site](https://webhook.site/) as a
disposable callback target.

1. Open <https://webhook.site> and copy the unique URL it gives you,
   e.g. `https://webhook.site/e9afcb42-4a99-45f3-a92d-dec5d1dd19c5`.
2. **Send that URL to Recruit Wizard support** and ask them to add it as an
   allowed redirect URI on your client. Recruit Wizard validates the
   `redirect_uri` you send in the authorize request against this white-list,
   so the authorize step will fail until support has registered it.
3. In `.env`, set `REDIRECT_URI` to the same URL so the sample builds an
   authorize link that matches what you registered:

   ```dotenv
   REDIRECT_URI=https://webhook.site/e9afcb42-4a99-45f3-a92d-dec5d1dd19c5
   ```

4. Once support has confirmed the URL is white-listed, `npm start`, open
   <http://localhost:3000>, click **Connect to Recruit Wizard**, and sign
   in. Recruit Wizard will redirect to webhook.site instead of back to the
   sample app.
5. In the webhook.site tab, look at the latest request's query string —
   you'll see `?code=…&state=…`. Copy the `code` value.
6. Switch back to the sample app's main tab, paste the code into the
   **Exchange code for token** input, and submit.

> The local `/auth/callback` page is skipped in this mode (the redirect lands
> on webhook.site, not on your machine), so the built-in `state` check is
> bypassed too. That's fine for ad-hoc testing — just don't ship a flow like
> this to production.

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

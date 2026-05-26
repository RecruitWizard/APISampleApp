# Recruit Wizard API – Auth Flow Sample

A sample application demonstrating the **OAuth 2.0 authorization code flow**
used by the [Recruit Wizard API](https://sandbox.recruitwizard.site/api-docs/#description/authentication).

The sample walks through the end-to-end flow:

1. Redirect the user to `GET {HOST}/api/connect/authorize`
2. Receive the authorization `code` on the registered redirect URI
3. Exchange the code for `access_token` + `refresh_token` via
   `POST {HOST}/api/connect/token` (`grant_type=authorization_code`)
4. Refresh access tokens using the refresh token
   (`grant_type=refresh_token`)
5. Call a protected endpoint with `Authorization: Bearer <access_token>`

## Samples in this repo

| Folder                | Stack                          | Status   |
| --------------------- | ------------------------------ | -------- |
| [`nodejs/`](./nodejs) | Node.js + Express + EJS        | Working  |

## Prerequisites

- A Recruit Wizard API client (`client_id` + `client_secret`) issued for the
  tenant you want to connect to.
- A registered **redirect URI** on that client that matches what the sample
  uses locally (default `http://localhost:3000/auth/callback`).

## Quick start

```bash
cd nodejs
npm install
cp .env.example .env
# edit .env: HOST, CLIENT_ID, CLIENT_SECRET, REDIRECT_URI
npm start
# open http://localhost:3000
```

See [`nodejs/README.md`](./nodejs/README.md) for the full walkthrough.

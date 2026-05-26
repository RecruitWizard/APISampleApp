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

| Folder                | Stack                                        | Status   |
| --------------------- | -------------------------------------------- | -------- |
| [`nodejs/`](./nodejs) | Node.js + Express + EJS                      | Working  |
| [`dotnet/`](./dotnet) | ASP.NET Core 9 + Razor Pages + minimal APIs  | Working  |

## Prerequisites

- A Recruit Wizard API client (`client_id` + `client_secret`) issued for the
  tenant you want to connect to.
- A **redirect URI** that Recruit Wizard support has white-listed on your
  client. For testing, the samples default to a
  `https://webhook.site/<uuid>` URL — see each sample's README for the
  full webhook.site walkthrough. (`localhost` callbacks work too if you
  already have one registered, but most setups won't.)

## Quick start

### Node.js

```bash
cd nodejs
npm install
cp .env.example .env
# edit .env: HOST, CLIENT_ID, CLIENT_SECRET, REDIRECT_URI
npm start
# open http://localhost:3000
```

See [`nodejs/README.md`](./nodejs/README.md) for the full walkthrough.

### .NET

```bash
cd dotnet
dotnet restore
# Configure RecruitWizard:Host, ClientId, ClientSecret, RedirectUri
# (appsettings.json, user secrets, or environment variables)
dotnet run
# open http://localhost:3000
```

See [`dotnet/README.md`](./dotnet/README.md) for the full walkthrough.

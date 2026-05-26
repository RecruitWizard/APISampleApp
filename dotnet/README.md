# Recruit Wizard API – Auth Flow Sample (.NET)

An ASP.NET Core (Razor Pages + minimal APIs) port of the Node.js sample,
demonstrating the **OAuth 2.0 authorization code flow** used by the
[Recruit Wizard API](https://sandbox.recruitwizard.site/api-docs/#description/authentication).

> This is one of several language samples in this repository. See the
> [top-level README](../README.md) for the full list.

## What it shows

1. Redirect the user to `GET {Host}/api/connect/authorize` (opened in a new tab)
2. Recruit Wizard redirects back to the registered callback URL, where the
   sample displays the authorization `code` on a copy-friendly page
3. Paste the code into the input on the main tab and exchange it for tokens
   via `POST {Host}/api/connect/token` (`grant_type=authorization_code`)
4. Refresh access tokens with `grant_type=refresh_token`
5. Call protected endpoints (`/api/ServerStatus`, `/api/Users/{id}`) using
   the access token in an `Authorization: Bearer …` header

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) (or newer)
- A Recruit Wizard API client (`ClientId` + `ClientSecret`) with a redirect
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

From this folder (`dotnet/`):

```bash
dotnet restore
# Configure your client (pick ONE of the options below — see "Configuration")
dotnet run
```

Then open <http://localhost:3000>.

> The default `applicationUrl` is `http://localhost:3000` (the local server
> the browser opens to use the sample UI — **not** the OAuth redirect URI).
> If port 3000 is already in use, pass `--urls http://localhost:5050` or
> similar; the redirect URI is independent.

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
3. Point the sample at the same URL so the authorize link it builds matches
   what you registered. For example, with user secrets:

   ```bash
   dotnet user-secrets set "RecruitWizard:RedirectUri" "https://webhook.site/e9afcb42-4a99-45f3-a92d-dec5d1dd19c5"
   ```

   Or in `appsettings.json`:

   ```json
   "RecruitWizard": {
     "RedirectUri": "https://webhook.site/e9afcb42-4a99-45f3-a92d-dec5d1dd19c5"
   }
   ```

4. Once support has confirmed the URL is white-listed, `dotnet run`, open
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

## Configuration

The sample reads its OAuth client config from the standard ASP.NET Core
configuration pipeline. Use whichever option you prefer:

### Option 1 — `appsettings.json` (quick start)

Edit the `RecruitWizard` section:

```json
{
  "RecruitWizard": {
    "Host": "https://sandbox.recruitwizard.site",
    "ClientId": "your-client-id-here",
    "ClientSecret": "your-client-secret-here",
    "RedirectUri": "https://webhook.site/your-unique-id-here"
  }
}
```

### Option 2 — User secrets (recommended for development)

Keeps your client secret out of source control:

```bash
dotnet user-secrets init
dotnet user-secrets set "RecruitWizard:ClientId"     "your-client-id-here"
dotnet user-secrets set "RecruitWizard:ClientSecret" "your-client-secret-here"
dotnet user-secrets set "RecruitWizard:Host"         "https://sandbox.recruitwizard.site"
dotnet user-secrets set "RecruitWizard:RedirectUri"  "https://webhook.site/your-unique-id-here"
```

### Option 3 — Environment variables

Names use the standard `:` → `__` mapping:

```powershell
$env:RecruitWizard__Host         = "https://sandbox.recruitwizard.site"
$env:RecruitWizard__ClientId     = "your-client-id-here"
$env:RecruitWizard__ClientSecret = "your-client-secret-here"
$env:RecruitWizard__RedirectUri  = "https://webhook.site/your-unique-id-here"
dotnet run
```

See [`.env.example`](./.env.example) for the full list.

## Configuration keys

| Setting                       | Description                                                                       |
| ----------------------------- | --------------------------------------------------------------------------------- |
| `RecruitWizard:Host`          | Base URL of the Recruit Wizard API (e.g. `https://sandbox.recruitwizard.site`)    |
| `RecruitWizard:ClientId`      | OAuth client ID issued by Recruit Wizard                                          |
| `RecruitWizard:ClientSecret`  | OAuth client secret issued by Recruit Wizard                                      |
| `RecruitWizard:RedirectUri`   | Callback URL — must exactly match one of the URLs registered with your client     |
| `ASPNETCORE_URLS` (optional)  | Override the listening URL/port (default `http://localhost:3000`)                 |

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
| `/assets/*`           | GET    | Static files served from `wwwroot/assets/` (logo, etc.)                       |

## Project layout

```
dotnet/
├── RecruitWizard.AuthSample.csproj
├── Program.cs                      # Routes, DI wiring, startup logging
├── appsettings.json                # Configuration (RecruitWizard section)
├── appsettings.Development.json
├── .env.example
├── Properties/
│   └── launchSettings.json
├── Services/
│   ├── AuthService.cs              # Authorize URL, token + refresh calls, JWT decode
│   ├── RecruitWizardOptions.cs     # Strongly typed config (validated on startup)
│   ├── StateStore.cs               # CSRF state generation/validation
│   └── TokenStore.cs               # In-memory token + last-call storage
├── Pages/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Index.cshtml                # Home page (paste-code UI)
│   ├── Index.cshtml.cs
│   ├── Callback.cshtml             # Page shown after Recruit Wizard redirects back
│   └── Callback.cshtml.cs
└── wwwroot/
    └── assets/
        └── full-logo-dark.png
```

## Notes

- Tokens are kept **in memory only**. Restart the process and you start fresh.
  In a real application, persist them per-user in a database (encrypted at rest).
- The `state` parameter is generated per login attempt and validated on the
  callback to mitigate CSRF.
- The JWT is decoded with a small base64url helper for display purposes only —
  the signature is **not** verified. For real authentication, use
  `Microsoft.AspNetCore.Authentication.JwtBearer` against Recruit Wizard's JWKS.
- The code is intentionally framework-light so it's easy to lift into your own app.

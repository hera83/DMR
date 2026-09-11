# DMR

An ASP.NET Core Web API that ingests vehicle data from the Danish Motor Register (**Motorstyrelsen's DMR** —
"Digital Motorregister"), keeps a local queryable copy of it, and exposes it over an authenticated HTTP API.

A background worker periodically connects to Motorstyrelsen's FTP export (see `.docs/`), downloads the newest
`ESStatistikListeModtag` export, converts it into a local SQLite dataset, and keeps that dataset up to date —
so lookups are served from disk instead of hitting the register on every request.

## Features

- **Vehicle lookup** by Danish registration number (`GET /Dmr/GetVehicleAsync/{registreringNummer}`).
- **Automatic ingest**, four times a day (00:00 / 06:00 / 12:00 / 18:00 local time): list the FTP export →
  skip if unchanged → download → unpack → convert to SQLite → register as the active dataset → prune old
  datasets. See [bgServices/DmrWorker.cs](bgServices/DmrWorker.cs).
- **API-key authentication** on every endpoint by default, with a separate Admin-only master key for managing
  keys (`/Key/*`: create, list, revoke, roll over).
- **Swagger UI** at `/swagger` (a bare `GET /` redirects there).

## Tech stack

- [.NET 10](https://dotnet.microsoft.com/) / ASP.NET Core Web API
- Entity Framework Core + SQLite for persistence (`app_dbs/`) — identity/API keys and the ingested DMR dataset
- [FluentFTP](https://github.com/robinrodricks/FluentFTP) for the FTP integration
- ASP.NET Core Identity (`AddIdentityCore`) backing API-key/Admin-role authentication

## Project layout

```
controllers/    [ApiController] endpoints (DmrController, KeyController)
services/       one folder per integration/business service (ftp, dmr, identity) — see services/<name>/docs
data/           shared EF Core DbContext, migrations, seeds, models — see data/README.md
bgServices/     hosted background services (DmrWorker — the scheduled ingest)
app_dbs/        SQLite database files (gitignored data, created at runtime)
app_files/      FTP downloads/raws/scratch directories (gitignored data, created at runtime)
```

## Getting started (local development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- FTP credentials for the Motorstyrelsen export (see `.docs/`)

### Configure

`appsettings.json` ships with empty placeholders for `Identity` and `Ftp`. For local development, create
`appsettings.Development.json` (already gitignored, so it never gets committed) with the same shape and fill
in real values:

```json
{
  "Identity": {
    "MasterKey": "<a long random secret>",
    "HeaderName": "X-Api-Key"
  },
  "Ftp": {
    "Host": "<ftp host>",
    "Port": 21,
    "Username": "<ftp username>",
    "Password": "<ftp password>",
    "UseEncryption": false,
    "RemoteBasePath": "/"
  }
}
```

### Run

```bash
dotnet build
dotnet run
```

The API listens on `http://localhost:5135` (see `properties/launchSettings.json`) with Swagger UI at
`http://localhost:5135/swagger`. On startup it applies pending EF Core migrations and seeds the fixed
Admin/ApiKeyHolder roles into `app_dbs/identity.db`.

Try the sample request in [DMR.http](DMR.http) directly from VS Code / Rider / Visual Studio.

## Deploying with Docker

### Prerequisites

- Docker + Docker Compose

### Steps

1. Copy the environment template and fill in your real secrets:

   ```bash
   cp .env.example .env
   ```

   Fill in `Identity__MasterKey` (generate one with e.g. `openssl rand -base64 32`) and the `Ftp__*` values for
   your Motorstyrelsen FTP access.

2. Build and start the container:

   ```bash
   docker compose up -d --build
   ```

3. The API is now available at `http://<server>:8080` (or whatever `PORT` you set in `.env`) — Swagger UI at
   `/swagger`.

4. `app_dbs/` and `app_files/` are bind-mounted into the container, so the SQLite databases and ingested data
   survive container rebuilds/upgrades. Neither needs to exist beforehand — on a fresh checkout the container
   creates the full folder structure itself on startup (see `entrypoint.sh`).

> The container only serves plain HTTP. Put a reverse proxy (Caddy, Nginx, Traefik, …) in front of it on your
> server to terminate TLS/HTTPS for a public deployment.

To update after pulling new code: `docker compose up -d --build` again — the persisted volumes are untouched.

## Authentication

Every endpoint requires a valid API key by default, sent in the header configured by `Identity:HeaderName`
(`X-Api-Key` by default):

- **Regular API keys** are created via `POST /Key/Create`, which itself requires the **master key** — the raw
  key is returned exactly once in that response and never shown again (only its hash is persisted).
- **Master key** (`Identity:MasterKey` / `Identity__MasterKey`) authenticates as the `Admin` role, the only
  role allowed to call `/Key/*` endpoints. No regular API key, however privileged, can create, list, revoke,
  or roll over keys.

## API overview

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/Dmr/GetVehicleAsync/{registreringNummer}` | API key | Look up a vehicle by registration number in the newest ingested dataset |
| POST | `/Key/Create` | Master key (Admin) | Issue a new API key |
| GET | `/Key/List` | Master key (Admin) | List issued API keys (`?includeRevoked=true` to include revoked ones) |
| DELETE | `/Key/Revoke/{id}` | Master key (Admin) | Revoke a key permanently |
| POST | `/Key/Rollover/{id}` | Master key (Admin) | Generate a new secret for an existing key (id/name/history unchanged) |

## License

[Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)](LICENSE.txt).

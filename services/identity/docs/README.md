# Identity service

Full ASP.NET Core Identity (`UserManager`/`RoleManager`, EF Core-backed) used to authenticate every request
on this API via a single `X-Api-Key` header — no password/cookie login exists or is planned. See
"Service structure conventions" in the repo root `CLAUDE.md` for the general shape this follows.

## Why Identity for something that's "just API keys"

The project owner explicitly asked for the real `Microsoft.AspNetCore.Identity` framework rather than a
bespoke API-key table, so that's what this is: every API key is owned by an auto-created `ApplicationUser`
(no password, never logs in) placed in the `ApiKeyHolder` role via `RoleManager`/`UserManager`, and
`[Authorize(Roles = ...)]` on controllers drives off those Identity roles like any normal Identity app. The
one deliberate deviation: the **master key** (from config, see below) authenticates as a synthetic `Admin`
principal with **no** stored `ApplicationUser` row — it's a bootstrap secret, not a "user", so there's
nothing to create, list, or accidentally delete via `UserManager`.

## Files

This service needs more than the standard four buckets (`docs/`, `dtos/`, `interfaces/`,
`<Name>Service.cs`) because it wraps an ASP.NET Core authentication scheme, which is neither a DTO, the
interface, nor the service's own business-logic file. One extra subfolder holds that, documented here so
it doesn't look like drift from the convention — see below for `authentication/`.

The EF Core persistence side (DbContext, migrations, seed data, table models) used to live under a
`services/identity/data/` subfolder here, but has since moved out to a **repo-root `data/` folder** — see
`data/README.md`. It isn't specific to this service: it's the one shared database any current or future
service's entities/migrations go into, so it sits as a sibling to `services/`, not nested inside one
service. `IdentityService.cs` still depends on it (`DMR.Data`/`DMR.Data.Models`/`DMR.Data.Seeds`), same as
it would depend on any other injected dependency.

- `interfaces/IIdentityService.cs` — the one interface.
- `IdentityService.cs` — the one implementation (service folder root, next to `docs/`, `dtos/`,
  `interfaces/`, not nested).
- `dtos/` — request/response pairs for every `IIdentityService` call (`CreateApiKey`, `ListApiKeys`,
  `RevokeApiKey`, `RolloverApiKey`, `ValidateApiKey`), plus three documented exceptions to the pairing rule:
  - `ApiKeyAuthOptions.cs` — config model bound from the `"Identity"` section (master key + header name).
  - `IdentityRoleNames.cs` — the two role-name constants (`Admin`, `ApiKeyHolder`).
  - `ApiKeyDto.cs` — the metadata item type nested inside `ListApiKeysResponseDto`.
- `authentication/` — the ASP.NET Core authentication scheme, not DTOs or the service implementation:
  - `ApiKeyAuthenticationOptions.cs` — marker options type for the `"ApiKey"` scheme.
  - `ApiKeyAuthenticationHandler.cs` — reads the header, calls `IIdentityService.ValidateApiKeyAsync`,
    builds the `ClaimsPrincipal` (role claim drives `[Authorize(Roles = ...)]`).

## Database

SQLite at `app_dbs/identity.db` — a **fixed** path resolved in `Program.cs`
(`IHostEnvironment.ContentRootPath` + `app_dbs/identity.db`), same pattern as
`DmrService.GetDatabasePath()`. Not configurable, and never touches the other files already in `app_dbs/`
(e.g. `dmr_<date>.db`) — those belong to the `dmr` service and are off limits to this one.

Schema is applied with `AppIdentityDbContext.Database.MigrateAsync()` at startup (see `Program.cs`) from
real EF Core migrations under `data/migrations/` — see `data/README.md` for the layout and how to add a
migration. Re-running the app against a missing/deleted `identity.db` recreates it from that history.

## Configuration

`"Identity"` section (see `appsettings.json` for the placeholder shape):

```json
"Identity": {
  "MasterKey": "",
  "HeaderName": "X-Api-Key"
}
```

- `MasterKey` — the one credential with the `Admin` role, and therefore the only credential
  `KeyController` accepts. **Never commit a real value to `appsettings.json`** — it's set in
  `appsettings.Development.json` (gitignored once a `.gitignore` exists) for local dev; use user secrets
  or an environment variable in any shared environment. An empty value disables master-key login entirely
  — `IdentityService.IsMasterKeyMatch` short-circuits on an empty configured key, so an unset master key
  can never accidentally match an empty/missing header.
- `HeaderName` — defaults to `X-Api-Key`; change only if a caller can't use that header name.

## Registration

Wired up in `Program.cs`: `AddDbContext<AppIdentityDbContext>`, `AddIdentityCore<ApplicationUser>()` +
`.AddRoles<IdentityRole>()` + `.AddEntityFrameworkStores<AppIdentityDbContext>()` (deliberately
`AddIdentityCore`, not `AddIdentity` — the latter also wires up cookie authentication as a second scheme,
which this API doesn't want), `AddScoped<IIdentityService, IdentityService>`, then the `"ApiKey"`
authentication scheme via `AddAuthentication().AddScheme<...>()`, and an `AuthorizationOptions.FallbackPolicy`
requiring an authenticated user — **every** endpoint requires a valid API key unless explicitly marked
`[AllowAnonymous]`. `KeyController` layers `[Authorize(Roles = IdentityRoleNames.Admin)]` on top, so only
the master key can reach it.

`app.Services.CreateScope()` right after `builder.Build()` applies pending migrations and calls
`IIdentityService.EnsureSeededAsync()` (which delegates the actual role creation to
`data/seeds/IdentityRoleSeeder.cs`) before the app starts serving requests.

## Usage

Every request must carry a valid key in the `X-Api-Key` header — the master key, or a key returned once by
`POST /Key/Create` (or `/Key/Rollover/{id}`):

```
GET /weatherforecast/
X-Api-Key: dmr_<a regular or master key>
```

`KeyController` is action-routed (`[Route("[controller]/[action]")]`, so `/Key/<Action>[/{id}]`), master
key only:

- `POST /Key/Create` — `{ "name": "n8n integration", "contactInfo": "someone@example.com",
  "expiresAtUtc": null }` → `201` with the raw key in `apiKey`. **Shown exactly once** — only its SHA-256
  hash is persisted, so a lost key can only be rolled over or revoked-and-recreated, never recovered.
  `contactInfo` is free text (email, phone, Slack handle, ...) for who to reach about this key; both it and
  `name` are optional at the type level (no validation attributes, matching the rest of this codebase) but
  should always be set in practice so a listed key is traceable to an owner.
- `GET /Key/List?includeRevoked=false` — lists key metadata (id, name, contact info, prefix, timestamps
  including `rolledOverAtUtc`, `isActive`); no key material.
- `POST /Key/Rollover/{id}` — issues fresh secret material for an existing key: same response shape as
  create (raw key shown once), but keeps the key's id/name/contactInfo/expiry/usage history — only the
  hash/prefix and `rolledOverAtUtc` change, and `lastUsedAtUtc` resets to null since the new secret hasn't
  been used yet. The old raw key is invalid the instant this returns. Fails (`404` + `success: false`) on
  an unknown id or an already-revoked key — a revoked key must be replaced with `POST /Key/Create` instead.
- `DELETE /Key/Revoke/{id}` — revokes a key. Permanent and idempotent (revoking twice is a no-op, not an
  error). A revoked key can never be rolled over again.

## Known gaps / follow-ups

- No automated tests yet for this service (`DMR.Tests` covers `dmr`/zip extraction only). Manually
  verified end-to-end: unauthenticated → 401, master key → 200 on both `/weatherforecast` and `/Key/List`,
  freshly created regular key → 200 on `/weatherforecast` but 403 on `/Key/List`, revoked key → 401,
  rollover → old raw key 401 / new raw key 200 / id+name+contactInfo unchanged, rollover of a revoked key →
  404, Swagger UI still reachable without a key in Development (it's classic middleware, registered before
  `UseAuthentication`/`UseAuthorization`, so it never hits the fallback policy), and a fresh
  `dotnet run` against a deleted `identity.db` correctly recreates it via `MigrateAsync()`.

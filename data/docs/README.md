# Data layer

The app's single persistence layer — one shared EF Core `DbContext`, its migrations, its seed data, and its
table models. Lives at the repo root, as a sibling to `controllers/` and `services/`, rather than under any
one `services/<name>/` folder, because it isn't specific to the `identity` service: it's the app-wide store
that any current or future service's entities/migrations go into. (`services/identity/` still owns the
business logic — key generation, validation, the `Admin`/`ApiKeyHolder` roles as a *concept* — it just no
longer owns the database plumbing; see services/identity/docs/README.md.)

## Layout

- `AppIdentityDbContext.cs` — the one `DbContext`, at the folder root (mirrors how a service's
  `<Name>Service.cs` sits at its own service root, next to its subfolders). `IdentityDbContext<ApplicationUser>`
  plus the `ApiKeys` table. Named "App..." rather than plain `IdentityDbContext` to avoid colliding with the
  base Identity type it extends.
- `models/` — every table model (entity class) in the system database, so it's easy to see the whole schema
  at a glance:
  - `ApplicationUser.cs` — `IdentityUser` subclass; the Identity-framework owner of an API key (no
    password/login — see services/identity/docs).
  - `ApiKey.cs` — one issued API key: hash + prefix, name/contact info, owner, timestamps.
- `migrations/` — EF Core migration history. Re-running `Program.cs`'s startup
  `AppIdentityDbContext.Database.MigrateAsync()` against a missing or deleted `app_dbs/identity.db`
  recreates the whole schema from this history, table by table — that's the point of using real migrations
  here instead of `EnsureCreated`.
- `seeds/` — pre-filled data inserted into the database, as opposed to schema (that's `migrations/`):
  - `IdentityRoleSeeder.cs` — creates the `Admin`/`ApiKeyHolder` roles if they don't already exist. Run at
    startup via `IdentityService.EnsureSeededAsync()`, which owns *when* seeding happens (once, before the
    app starts serving); this class owns *what* gets seeded.

## Adding a migration

After changing a model in `models/` or `AppIdentityDbContext.OnModelCreating`, generate a migration with
the same flags every time — the defaults `dotnet ef` infers on this project have landed a migration in the
wrong folder before:

```
dotnet ef migrations add <Name> --context AppIdentityDbContext --output-dir data/migrations --namespace DMR.Data.Migrations
```

Then restart the app (or run `dotnet ef database update --context AppIdentityDbContext`) to apply it —
`Program.cs` already calls `MigrateAsync()` on every startup, so a normal `dotnet run` is enough in dev.

## Adding a seed

Add a new seeder class under `seeds/` (one class per kind of seed data, same one-thing-per-file spirit as
`services/<name>/dtos/`) and call it from `IdentityService.EnsureSeededAsync()` — or from wherever the
future service that owns that data lives, if it isn't `identity`-related.

## Database

SQLite at `app_dbs/identity.db` — a **fixed** path resolved in `Program.cs`
(`IHostEnvironment.ContentRootPath` + `app_dbs/identity.db`), same pattern as `DmrService.GetDatabasePath()`.
Not configurable, and never touches the other files already in `app_dbs/` (e.g. `dmr_<date>.db`) — those
belong to the `dmr` service and are off limits to this layer.

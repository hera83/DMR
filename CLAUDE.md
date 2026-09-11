# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This is a freshly scaffolded ASP.NET Core Web API (`dotnet new webapi`, target framework `net10.0`) — the
`WeatherForecastController`/`WeatherForecast` files are still the unmodified template sample. There is no git
repository initialized yet, no test project, and no README.

A couple of directories hint at the intended purpose but currently hold no application code:
- `.docs/` — a link to the Danish Motorstyrelsen (motor vehicle register) "andre adgange" (other access methods)
  page, suggesting this API will integrate with or process data from the Danish Motor Register.
- `app_files/raws/` — contains a raw data export zip (`ESStatistikListeModtag-*.zip`), likely a sample of data
  this service will ingest.
- `app_dbs/` — currently empty; presumably reserved for local database files.

Treat these as forward-looking context, not as established architecture — build structure as the actual
implementation emerges rather than assuming a shape for these folders.

## Commands

- Restore & build: `dotnet build`
- Run (Development profile, Swagger UI enabled): `dotnet run` — serves on `http://localhost:5135` (see
  `properties/launchSettings.json`; the `https` profile also serves `https://localhost:7140`)
- Swagger/OpenAPI UI is only wired up when `ASPNETCORE_ENVIRONMENT=Development` (see `Program.cs`)
- Try the sample endpoint with the checked-in request file: `DMR.http` (`GET /weatherforecast/`), runnable via
  the HTTP file support in VS Code/Rider/Visual Studio
- There is no test project yet; add one (e.g. `dotnet new xunit`) before writing tests, and reference it from a
  solution file if one is added

## Architecture

Standard minimal-hosting ASP.NET Core Web API layout:
- `Program.cs` — application entry point and service/middleware pipeline configuration (controllers, Swagger,
  HTTPS redirection, authorization). New middleware/services are registered here.
- `controllers/` — `[ApiController]` MVC controllers, one per resource, routed via `[Route("[controller]")]`.
- Plain model/DTO types (e.g. `WeatherForecast.cs`) currently live at the project root in the `DMR` namespace;
  controllers live in `DMR.Controllers`.
- `appsettings.json` / `appsettings.Development.json` — standard layered configuration.
- `services/<name>/` — integration/business logic services (e.g. `services/ftp/`), each self-contained. See
  "Service structure conventions" below — this structure is a firm project rule, not a one-off choice made
  for the first service.
- `data/` — the app's single shared persistence layer (one EF Core `DbContext`, its `migrations/`,
  `seeds/`, and `models/`), a sibling to `services/` rather than nested inside any one service, since the
  database isn't specific to one service. See `data/README.md`.

## Service structure conventions

Every service lives under `services/<name>/` (e.g. `services/ftp/`) and follows this exact layout —
apply it to every new service, not just the first one:

- `services/<name>/docs/` — Markdown notes aimed at an AI reading the codebase: what the service does, any
  setup/config it needs, gotchas, known gaps. Keep this updated as the service evolves.
- `services/<name>/dtos/` — exactly one class per file, always. Whenever a model represents something
  exchanged on a service call, it comes as a `<Thing>RequestDto` / `<Thing>ResponseDto` pair, each in its
  own file (`<Thing>RequestDto.cs`, `<Thing>ResponseDto.cs`) — never combined into one file. Config/options
  classes bound from `appsettings` and small supporting/nested item types that never travel on their own are
  the exception to the request/response *pairing* rule (they don't need a counterpart) — but they still each
  get their own file, and the exception should be noted in a doc comment.
- Anything that must always take a fixed value (a fixed local directory, a fixed remote root, etc.) is
  **not** a field on a request/response DTO — it's resolved inside the service implementation (a constant,
  or computed from injected config/host environment) and left out of the models entirely, so callers can't
  override it.
- `services/<name>/interfaces/` — exactly one file, `I<Name>Service.cs` (PascalCase, e.g. `IFtpService.cs`
  for the `ftp` service), defining exactly one interface.
- `services/<name>/<Name>Service.cs` — exactly one implementation file at the service folder root (not
  nested under a subfolder), e.g. `FtpService.cs` implementing `IFtpService`.
- Namespaces: `DMR.Services.<Name>` for the service class, `DMR.Services.<Name>.Dtos` for DTOs,
  `DMR.Services.<Name>.Interfaces` for the interface.
- Register the service's options (`builder.Services.Configure<...>`) and its interface→implementation
  mapping (`builder.Services.AddScoped<I<Name>Service, ...>`) in `Program.cs`.
- Prefer a well-maintained free/open-source library over hand-rolled protocol code when one exists (e.g.
  FluentFTP for `services/ftp/`) — check license and maintenance status first.

`services/ftp/` is the reference implementation of this structure — copy its shape for new services.

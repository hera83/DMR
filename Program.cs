using DMR.BgServices;
using DMR.Data;
using DMR.Data.Models;
using DMR.Services.Ftp.Dtos;
using DMR.Services.Ftp.Interfaces;
using DMR.Services.Dmr.Interfaces;
using DMR.Services.Health.Interfaces;
using DMR.Services.Identity.Authentication;
using DMR.Services.Identity.Dtos;
using DMR.Services.Identity.Interfaces;
using DMR.Services.Logs.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging — see services/logs/docs. MinimumLevel (and its per-category overrides) is read from
// the "Serilog" section in appsettings.json/appsettings.Development.json, so each environment can run at a
// different floor (e.g. Debug locally, Information in production) without a code change. The sinks
// themselves (Console + SQLite) are wired up here in code rather than via that same config section because
// the SQLite sink's file path/table name/rollover behaviour must always be fixed — see the comment above
// the WriteTo.SQLite call, and the project's "fixed values aren't config fields" convention.
var logDatabasePath = Path.Combine(builder.Environment.ContentRootPath, "app_dbs", "serilog.db");
Directory.CreateDirectory(Path.GetDirectoryName(logDatabasePath)!);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        // Fixed path/table/shape — never configurable per request or per environment, so exactly ONE
        // database file ever exists (app_dbs/serilog.db, alongside the app's other databases). Must stay
        // in sync with LogsService.GetDatabasePath(), which reads this same file directly to serve
        // LogController's search endpoint.
        //   - rollOver: false + a generous maxDatabaseSize (1 GB): the sink never creates a
        //     "<name>-yyyyMMdd_HHmmss-<guid>.db" sibling file — there is only ever one serilog.db.
        //   - retentionPeriod: keeps that single file from growing forever instead (old rows are pruned on
        //     a timer), which is what actually keeps "no rollover" safe long-term.
        //   - batchSize: 1 — flush every event immediately rather than buffering, so a log line is visible
        //     to LogController's search immediately after it's written, not after a batch fills up.
        .WriteTo.SQLite(
            sqliteDbPath: logDatabasePath,
            tableName: "Logs",
            storeTimestampInUtc: true,
            batchSize: 1,
            maxDatabaseSize: 1024,
            rollOver: false,
            retentionPeriod: TimeSpan.FromDays(90));
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Environment badge shown under the title on the Swagger UI landing page, so it's obvious at a glance
    // whether you're looking at Development or Production (or anything else ASPNETCORE_ENVIRONMENT is set
    // to). Rendered from Markdown in the doc's Description via a shields.io badge image — green for
    // Production, orange for Development, yellow for anything else (e.g. Staging).
    var environmentName = builder.Environment.EnvironmentName;
    var badgeColor = environmentName switch
    {
        "Production" => "brightgreen",
        "Development" => "orange",
        _ => "yellow"
    };
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "DMR API",
        Version = "v1",
        Description = $"![{environmentName}](https://img.shields.io/badge/env-{Uri.EscapeDataString(environmentName)}-{badgeColor})"
    });

    // Lets Swagger UI send the "X-Api-Key" header on requests, and documents it on every endpoint.
    options.AddSecurityDefinition(ApiKeyAuthenticationOptions.SchemeName, new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "API key (or the master key, for /Key endpoints). Sent in the X-Api-Key header."
    });
    // The reference must be bound to `document` (the OpenApiDocument being built) — constructing it with
    // just the scheme name, with no document, leaves it unresolved and Swashbuckle silently serializes the
    // requirement as an empty object ("security": [{}]), which makes Swagger UI never attach the
    // X-Api-Key header to "Try it out" requests even after clicking Authorize. See Microsoft.OpenApi 2.0's
    // breaking change removing Reference from OpenApiSecurityScheme (dotnet/aspnetcore#61123).
    options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference(ApiKeyAuthenticationOptions.SchemeName, document)] = []
    });
});

builder.Services.Configure<FtpConnectionOptions>(builder.Configuration.GetSection("Ftp"));
builder.Services.AddScoped<IFtpService, DMR.Services.Ftp.FtpService>();
builder.Services.AddScoped<IDmrService, DMR.Services.Dmr.DmrService>();
builder.Services.AddScoped<ILogsService, DMR.Services.Logs.LogsService>();
// No options to bind — see services/health/docs; the freshness thresholds are fixed constants, same as
// DmrWorker's own window/retention constants, not per-environment configuration.
builder.Services.AddScoped<IHealthService, DMR.Services.Health.HealthService>();

// Daily DMR ingest — runs once a day at 03:00 local time; see bgServices/DmrWorker.cs. Its
// AppIdentityDbContext/IFtpService/IDmrService dependencies are scoped, so the worker (a singleton) opens
// its own DI scope per run rather than taking them as constructor parameters directly.
builder.Services.AddHostedService<DmrWorker>();

// Identity + API-key authentication — see services/identity/docs. Backed by SQLite at a fixed path
// (app_dbs/identity.db), never configurable per the project's "fixed values aren't request fields"
// convention. Directory.CreateDirectory here mirrors DmrService.GetDatabasePath/ImportToSqlite, which
// makes the same guarantee for app_dbs before opening a connection.
var identityDbPath = Path.Combine(builder.Environment.ContentRootPath, "app_dbs", "identity.db");
Directory.CreateDirectory(Path.GetDirectoryName(identityDbPath)!);

builder.Services.Configure<ApiKeyAuthOptions>(builder.Configuration.GetSection("Identity"));
builder.Services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlite($"Data Source={identityDbPath}"));

builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppIdentityDbContext>();

builder.Services.AddScoped<IIdentityService, DMR.Services.Identity.IdentityService>();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationOptions.SchemeName)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.SchemeName, _ => { });

builder.Services.AddAuthorization(options =>
{
    // Every endpoint requires a valid API key by default unless it opts out with [AllowAnonymous].
    // KeyController additionally requires the Admin role via its own [Authorize(Roles = ...)] — see
    // controllers/KeyController.cs.
    options.FallbackPolicy = new AuthorizationPolicyBuilder(ApiKeyAuthenticationOptions.SchemeName)
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Applies pending EF Core migrations (see data/migrations/) to app_dbs/identity.db and seeds the fixed
// Admin/ApiKeyHolder roles (see data/seeds/). Re-running this against a missing/deleted identity.db
// recreates it from scratch, table by table, from the migration history — see data/README.md.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    await db.Database.MigrateAsync();

    var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
    await identityService.EnsureSeededAsync();
}

// Configure the HTTP request pipeline.
// Logs one structured line per request (method, path, status code, elapsed ms) into the same Console +
// SQLite sinks as everything else — placed first so it wraps the entire pipeline below, including the "/"
// redirect and Swagger.
app.UseSerilogRequestLogging();

// Swagger is intentionally enabled in every environment (not just Development) so the API stays
// self-documenting in production too — see the "/" redirect below.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // Collapse each controller group to a single line by default (e.g. "Key") — clicking it expands the
    // list of operations (Create, etc.) without auto-expanding their full request/response detail.
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    // -1 hides the "Schemas" section at the bottom of the page entirely; it's not meant to be browsed.
    options.DefaultModelsExpandDepth(-1);
});

// A bare GET / has nothing to serve on its own; send visitors straight to the Swagger UI so the API
// docs are discoverable without knowing the /swagger path up front. Plain middleware, not a mapped
// endpoint — it's just a redirect, so it never touches routing/endpoint metadata and skips the
// authentication/authorization pipeline entirely (no need for an AllowAnonymous exception).
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && HttpMethods.IsGet(context.Request.Method))
    {
        context.Response.Redirect("/swagger");
        return;
    }

    await next();
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

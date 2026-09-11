using DMR.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DMR.Data;

/// <summary>
/// EF Core store for ASP.NET Core Identity (users/roles/claims) plus the <see cref="ApiKey"/> table.
/// Backed by SQLite at app_dbs/identity.db — see <c>Program.cs</c> for how the connection string is
/// resolved (always that fixed path, never configurable per the project's "fixed values aren't request
/// fields" convention). See data/README.md for why this whole persistence layer lives at the repo root
/// rather than under services/identity, and how <c>models/</c>/<c>migrations/</c>/<c>seeds/</c> divide up.
/// Named "App..." rather than plain "IdentityDbContext" to avoid colliding with the base
/// <see cref="IdentityDbContext{TUser}"/> type it extends.
/// </summary>
public class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    /// <summary>Run log for <c>DmrWorker</c> — see <see cref="DmrWorkerRun"/>.</summary>
    public DbSet<DmrWorkerRun> DmrWorkerRuns => Set<DmrWorkerRun>();

    /// <summary>Registry of converted-and-ready DMR datasets — see <see cref="DmrDataset"/>.</summary>
    public DbSet<DmrDataset> DmrDatasets => Set<DmrDataset>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApiKey>(entity =>
        {
            entity.HasIndex(k => k.KeyHash).IsUnique();
            entity.HasOne(k => k.User)
                .WithMany()
                .HasForeignKey(k => k.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DmrDataset>(entity =>
        {
            // Every lookup of "the current dataset" orders by CreatedAtUtc descending (see DmrWorker and
            // DmrController) — index it so that stays cheap as app_dbs/DmrDatasets accumulates history
            // beyond the retained rows.
            entity.HasIndex(d => d.CreatedAtUtc);

            // DatabasePath is date-stamped from the source XML file's own last-write time (see
            // DmrService.GetDatabasePath), not from when the row is created — so two different ingests
            // could in principle compute the same path (e.g. the FTP source re-publishing a file whose
            // internal timestamp didn't change). Unique here as a hard backstop: DmrWorker.RunOnceAsync
            // already removes any existing row for the same path before inserting a new one (its
            // ConvertAsync call has, by definition, just overwritten that file from scratch, so an old row
            // still pointing at it would be describing stale data), but this index means a bug that skips
            // that step fails loudly (a save throws) instead of silently leaving two dataset rows pointing
            // at one file — which could otherwise cause retention cleanup to delete a file a newer row
            // still depends on.
            entity.HasIndex(d => d.DatabasePath).IsUnique();
        });
    }
}

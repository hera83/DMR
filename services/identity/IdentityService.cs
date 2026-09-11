using System.Security.Cryptography;
using System.Text;
using DMR.Data;
using DMR.Data.Models;
using DMR.Data.Seeds;
using DMR.Services.Identity.Dtos;
using DMR.Services.Identity.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DMR.Services.Identity;

/// <inheritdoc cref="IIdentityService"/>
public class IdentityService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    AppIdentityDbContext db,
    IOptions<ApiKeyAuthOptions> options,
    ILogger<IdentityService> logger)
    : IIdentityService
{
    /// <summary>Length in bytes of the random key material — 32 bytes (256 bits) before base64url
    /// encoding, well beyond what's brute-forceable.</summary>
    private const int KeyByteLength = 32;

    /// <summary>Prefix on every generated key, so a key found in a log/commit is recognizable at a
    /// glance as belonging to this API.</summary>
    private const string KeyPrefixTag = "dmr_";

    /// <summary>How many characters of the raw key are kept in the clear as <see cref="ApiKey.KeyPrefix"/>.</summary>
    private const int DisplayPrefixLength = 12;

    /// <summary>Synthetic identifier used as the ClaimTypes.NameIdentifier when a request authenticates
    /// with the master key. Not a real ApplicationUser id — the master key is a config secret with no
    /// stored row (see services/identity/docs).</summary>
    private const string MasterKeyPrincipalId = "master-key";

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        // The actual seed data/logic lives in data/seeds/IdentityRoleSeeder.cs — this method just owns
        // *when* it runs (once, at startup — see Program.cs) plus the non-seed-data startup check below.
        await IdentityRoleSeeder.SeedAsync(roleManager);

        if (string.IsNullOrWhiteSpace(options.Value.MasterKey))
            logger.LogWarning(
                "Identity:MasterKey is not set — no request can authenticate as Admin, so KeyController is unreachable until it's configured.");
    }

    public async Task<CreateApiKeyResponseDto> CreateApiKeyAsync(CreateApiKeyRequestDto request, CancellationToken cancellationToken = default)
    {
        var (rawKey, hash) = GenerateKey();

        // One ApplicationUser per key, created purely to be the Identity-framework owner of the key — see
        // data/models/ApplicationUser.cs. The username just needs to be unique; it's never shown to the
        // caller or used to sign in.
        var user = new ApplicationUser { UserName = $"apikey_{Guid.NewGuid():N}" };
        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(
                $"Failed to create the Identity user backing this API key: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");

        var roleResult = await userManager.AddToRoleAsync(user, IdentityRoleNames.ApiKeyHolder);
        if (!roleResult.Succeeded)
            throw new InvalidOperationException(
                $"Failed to grant {IdentityRoleNames.ApiKeyHolder} to the new API key's user: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");

        var entity = new ApiKey
        {
            UserId = user.Id,
            Name = request.Name,
            ContactInfo = request.ContactInfo,
            KeyHash = hash,
            KeyPrefix = rawKey[..Math.Min(DisplayPrefixLength, rawKey.Length)],
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = request.ExpiresAtUtc
        };
        db.ApiKeys.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created API key {KeyId} ({Name})", entity.Id, entity.Name);

        return new CreateApiKeyResponseDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ContactInfo = entity.ContactInfo,
            ApiKey = rawKey,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc
        };
    }

    public async Task<ListApiKeysResponseDto> ListApiKeysAsync(ListApiKeysRequestDto request, CancellationToken cancellationToken = default)
    {
        var query = db.ApiKeys.AsNoTracking().AsQueryable();
        if (!request.IncludeRevoked)
            query = query.Where(k => k.RevokedAtUtc == null);

        var now = DateTime.UtcNow;
        var keys = await query
            .OrderByDescending(k => k.CreatedAtUtc)
            .Select(k => new ApiKeyDto
            {
                Id = k.Id,
                Name = k.Name,
                ContactInfo = k.ContactInfo,
                KeyPrefix = k.KeyPrefix,
                CreatedAtUtc = k.CreatedAtUtc,
                ExpiresAtUtc = k.ExpiresAtUtc,
                RevokedAtUtc = k.RevokedAtUtc,
                LastUsedAtUtc = k.LastUsedAtUtc,
                RolledOverAtUtc = k.RolledOverAtUtc,
                IsActive = k.RevokedAtUtc == null && (k.ExpiresAtUtc == null || k.ExpiresAtUtc > now)
            })
            .ToListAsync(cancellationToken);

        return new ListApiKeysResponseDto { Keys = keys };
    }

    public async Task<RevokeApiKeyResponseDto> RevokeApiKeyAsync(RevokeApiKeyRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == request.KeyId, cancellationToken);
        if (entity is null)
            return new RevokeApiKeyResponseDto { Success = false, ErrorMessage = $"No API key found with id '{request.KeyId}'." };

        // Idempotent: revoking an already-revoked key just keeps its original revocation time.
        entity.RevokedAtUtc ??= DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Revoked API key {KeyId} ({Name})", entity.Id, entity.Name);
        return new RevokeApiKeyResponseDto { Success = true };
    }

    public async Task<RolloverApiKeyResponseDto> RolloverApiKeyAsync(RolloverApiKeyRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == request.KeyId, cancellationToken);
        if (entity is null)
            return new RolloverApiKeyResponseDto { Success = false, ErrorMessage = $"No API key found with id '{request.KeyId}'." };

        if (entity.RevokedAtUtc is not null)
            return new RolloverApiKeyResponseDto
            {
                Success = false,
                ErrorMessage = $"API key '{request.KeyId}' is revoked and can't be rolled over — create a new one instead."
            };

        var (rawKey, hash) = GenerateKey();
        entity.KeyHash = hash;
        entity.KeyPrefix = rawKey[..Math.Min(DisplayPrefixLength, rawKey.Length)];
        entity.RolledOverAtUtc = DateTime.UtcNow;
        // The old secret is now invalid; nothing has authenticated with the new one yet.
        entity.LastUsedAtUtc = null;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Rolled over API key {KeyId} ({Name})", entity.Id, entity.Name);

        return new RolloverApiKeyResponseDto
        {
            Success = true,
            Id = entity.Id,
            Name = entity.Name,
            ContactInfo = entity.ContactInfo,
            ApiKey = rawKey,
            RolledOverAtUtc = entity.RolledOverAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc
        };
    }

    public async Task<ValidateApiKeyResponseDto> ValidateApiKeyAsync(ValidateApiKeyRequestDto request, CancellationToken cancellationToken = default)
    {
        if (IsMasterKeyMatch(request.RawKey))
        {
            return new ValidateApiKeyResponseDto
            {
                IsValid = true,
                UserId = MasterKeyPrincipalId,
                KeyId = null,
                Role = IdentityRoleNames.Admin
            };
        }

        var hash = Hash(request.RawKey);
        var entity = await db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == hash, cancellationToken);

        var now = DateTime.UtcNow;
        if (entity is null || entity.RevokedAtUtc is not null || (entity.ExpiresAtUtc is not null && entity.ExpiresAtUtc <= now))
            return new ValidateApiKeyResponseDto { IsValid = false };

        entity.LastUsedAtUtc = now;
        await db.SaveChangesAsync(cancellationToken);

        return new ValidateApiKeyResponseDto
        {
            IsValid = true,
            UserId = entity.UserId,
            KeyId = entity.Id,
            Role = IdentityRoleNames.ApiKeyHolder
        };
    }

    /// <summary>Constant-time comparison against the configured master key — an empty configured value
    /// never matches, so master-key login is off by default until <see cref="ApiKeyAuthOptions.MasterKey"/>
    /// is set.</summary>
    private bool IsMasterKeyMatch(string rawKey)
    {
        var masterKey = options.Value.MasterKey;
        if (string.IsNullOrEmpty(masterKey))
            return false;

        var a = Encoding.UTF8.GetBytes(masterKey);
        var b = Encoding.UTF8.GetBytes(rawKey);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static (string RawKey, string Hash) GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(KeyByteLength);
        var rawKey = KeyPrefixTag + Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (rawKey, Hash(rawKey));
    }

    private static string Hash(string rawKey) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
}

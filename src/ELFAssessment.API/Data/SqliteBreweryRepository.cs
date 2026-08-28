using ELFAssessment.API.Models;
using ELFAssessment.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ELFAssessment.API.Data;

/// <summary>SQLite-backed repository with in-memory caching.</summary>
public sealed class SqliteBreweryRepository : IBreweryRepository
{
    private const string CacheKey = "breweries_sqlite_all";
    private readonly BreweryDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SqliteBreweryRepository> _logger;
    private readonly TimeSpan _cacheDuration;

    public SqliteBreweryRepository(
        BreweryDbContext db,
        IMemoryCache cache,
        ILogger<SqliteBreweryRepository> logger,
        Microsoft.Extensions.Options.IOptions<Configuration.BreweryDataOptions> options)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
        _cacheDuration = options.Value.CacheDuration;
    }

    public async Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<Brewery>? cached) && cached is not null)
            return cached;

        _logger.LogInformation("Cache miss – loading breweries from SQLite");
        var entities = await _db.Breweries.AsNoTracking().ToListAsync(cancellationToken);
        var breweries = entities.Select(MapToDomain).ToList().AsReadOnly();

        _cache.Set(CacheKey, breweries, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _cacheDuration
        });

        return breweries;
    }

    public async Task<Brewery?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Breweries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    private static Brewery MapToDomain(BreweryEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        BreweryType = e.BreweryType,
        Address = e.Address,
        City = e.City,
        StateProvince = e.StateProvince,
        PostalCode = e.PostalCode,
        Country = e.Country,
        Longitude = e.Longitude,
        Latitude = e.Latitude,
        Phone = e.Phone,
        WebsiteUrl = e.WebsiteUrl
    };
}

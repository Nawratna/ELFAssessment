using ELFAssessment.API.Configuration;
using ELFAssessment.API.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ELFAssessment.API.Services;

/// <summary>
/// In-memory repository that loads data from the source API and caches it.
/// Uses SemaphoreSlim to prevent cache stampede.
/// </summary>
public sealed class InMemoryBreweryRepository : IBreweryRepository
{
    private const string CacheKey = "breweries_all";
    private readonly IBrewerySourceLoader _loader;
    private readonly IMemoryCache _cache;
    private readonly BreweryDataOptions _options;
    private readonly ILogger<InMemoryBreweryRepository> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public InMemoryBreweryRepository(
        IBrewerySourceLoader loader,
        IMemoryCache cache,
        IOptions<BreweryDataOptions> options,
        ILogger<InMemoryBreweryRepository> logger)
    {
        _loader = loader;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<Brewery>? cached) && cached is not null)
            return cached;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue(CacheKey, out cached) && cached is not null)
                return cached;

            _logger.LogInformation("Cache miss – loading breweries from source");
            var sources = await _loader.LoadAsync(cancellationToken);
            var breweries = sources.Select(BreweryMapper.ToDomain).ToList().AsReadOnly();

            _cache.Set(CacheKey, breweries, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _options.CacheDuration
            });

            _logger.LogInformation("Cached {Count} breweries for {Duration}", breweries.Count, _options.CacheDuration);
            return breweries;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Brewery?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}

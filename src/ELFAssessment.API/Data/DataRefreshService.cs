using ELFAssessment.API.Configuration;
using ELFAssessment.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ELFAssessment.API.Data;

/// <summary>Refreshes the SQLite database from the source API on a configurable interval (default: 10 minutes).</summary>
public sealed class DataRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval;
    private readonly ILogger<DataRefreshService> _logger;

    public DataRefreshService(IServiceScopeFactory scopeFactory, IOptions<BreweryDataOptions> options, ILogger<DataRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _interval = options.Value.CacheDuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait one full interval before the first refresh (initial seed already happened at startup)
        await Task.Delay(_interval, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Refreshing brewery data from source API…");
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BreweryDbContext>();
                var loader = scope.ServiceProvider.GetRequiredService<IBrewerySourceLoader>();

                var sources = await loader.LoadAsync(stoppingToken);
                var entities = sources.Select(s => new BreweryEntity
                {
                    Id = s.Id,
                    Name = s.Name,
                    BreweryType = s.BreweryType,
                    Address = BuildAddress(s.Address1, s.Address2, s.Address3),
                    City = s.City,
                    StateProvince = s.StateProvince,
                    PostalCode = s.PostalCode,
                    Country = s.Country,
                    Longitude = s.Longitude,
                    Latitude = s.Latitude,
                    Phone = s.Phone,
                    WebsiteUrl = s.WebsiteUrl
                }).ToList();

                await db.Database.ExecuteSqlRawAsync("DELETE FROM Breweries", stoppingToken);
                db.Breweries.AddRange(entities);
                await db.SaveChangesAsync(stoppingToken);

                // Evict the cache so the next read picks up fresh data
                var cache = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                cache.Remove("breweries_sqlite_all");

                _logger.LogInformation("Refreshed {Count} breweries in SQLite", entities.Count);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh brewery data – will retry next interval");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private static string? BuildAddress(string? a1, string? a2, string? a3)
    {
        var parts = new[] { a1, a2, a3 }.Where(p => !string.IsNullOrWhiteSpace(p));
        var joined = string.Join(", ", parts);
        return string.IsNullOrEmpty(joined) ? null : joined;
    }
}

using ELFAssessment.API.Services;
using Microsoft.EntityFrameworkCore;

namespace ELFAssessment.API.Data;

/// <summary>Seeds the SQLite database from the Open Brewery DB API on first run.</summary>
public sealed class DatabaseInitializer
{
    private readonly BreweryDbContext _db;
    private readonly IBrewerySourceLoader _loader;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(BreweryDbContext db, IBrewerySourceLoader loader, ILogger<DatabaseInitializer> logger)
    {
        _db = db;
        _loader = loader;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _db.Database.EnsureCreatedAsync(cancellationToken);

        if (await _db.Breweries.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Database already seeded – skipping");
            return;
        }

        _logger.LogInformation("Seeding database from Open Brewery DB API…");
        var sources = await _loader.LoadAsync(cancellationToken);
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

        _db.Breweries.AddRange(entities);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} breweries into SQLite", entities.Count);
    }

    private static string? BuildAddress(string? a1, string? a2, string? a3)
    {
        var parts = new[] { a1, a2, a3 }.Where(p => !string.IsNullOrWhiteSpace(p));
        var joined = string.Join(", ", parts);
        return string.IsNullOrEmpty(joined) ? null : joined;
    }
}

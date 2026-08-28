using ELFAssessment.API.Configuration;
using ELFAssessment.API.Data;
using ELFAssessment.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ELFAssessment.Tests;

public class SqliteBreweryRepositoryTests : IDisposable
{
    private readonly BreweryDbContext _db;
    private readonly SqliteBreweryRepository _repository;
    private readonly IMemoryCache _cache;

    public SqliteBreweryRepositoryTests()
    {
        var dbOptions = new DbContextOptionsBuilder<BreweryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new BreweryDbContext(dbOptions);
        _cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new BreweryDataOptions { CacheDuration = TimeSpan.FromMinutes(10) });
        var logger = Mock.Of<ILogger<SqliteBreweryRepository>>();
        _repository = new SqliteBreweryRepository(_db, _cache, logger, options);

        SeedData();
    }

    private void SeedData()
    {
        _db.Breweries.AddRange(
            new BreweryEntity
            {
                Id = "1", Name = "Test Alpha", BreweryType = "micro",
                Address = "123 Main St", City = "Portland", StateProvince = "Oregon",
                PostalCode = "97201", Country = "United States",
                Latitude = 45.52, Longitude = -122.68,
                Phone = "5031234567", WebsiteUrl = "https://alpha.com"
            },
            new BreweryEntity
            {
                Id = "2", Name = "Test Beta", BreweryType = "brewpub",
                City = "Austin", StateProvince = "Texas",
                PostalCode = "73301", Country = "United States"
            }
        );
        _db.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllBreweries()
    {
        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_MapsEntityToDomain()
    {
        var result = await _repository.GetAllAsync();
        var alpha = result.First(b => b.Id == "1");

        Assert.Equal("Test Alpha", alpha.Name);
        Assert.Equal("micro", alpha.BreweryType);
        Assert.Equal("123 Main St", alpha.Address);
        Assert.Equal("Portland", alpha.City);
        Assert.Equal(45.52, alpha.Latitude);
    }

    [Fact]
    public async Task GetAllAsync_CachesResults()
    {
        var first = await _repository.GetAllAsync();

        // Add more data after initial load
        _db.Breweries.Add(new BreweryEntity
        {
            Id = "3", Name = "New", City = "Denver", StateProvince = "CO",
            PostalCode = "80201", Country = "US", BreweryType = "micro"
        });
        await _db.SaveChangesAsync();

        var second = await _repository.GetAllAsync();

        // Should still return cached result (2 items, not 3)
        Assert.Equal(first.Count, second.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsBrewery()
    {
        var result = await _repository.GetByIdAsync("1");

        Assert.NotNull(result);
        Assert.Equal("Test Alpha", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync("nonexistent");

        Assert.Null(result);
    }

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
    }
}

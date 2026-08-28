using ELFAssessment.API.Configuration;
using ELFAssessment.API.Models;
using ELFAssessment.API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace ELFAssessment.Tests;

public class InMemoryBreweryRepositoryTests
{
    private readonly Mock<IBrewerySourceLoader> _loaderMock;
    private readonly IMemoryCache _cache;
    private readonly InMemoryBreweryRepository _repository;

    public InMemoryBreweryRepositoryTests()
    {
        _loaderMock = new Mock<IBrewerySourceLoader>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new BreweryDataOptions { CacheDuration = TimeSpan.FromMinutes(10) });
        var logger = Mock.Of<ILogger<InMemoryBreweryRepository>>();
        _repository = new InMemoryBreweryRepository(_loaderMock.Object, _cache, options, logger);
    }

    [Fact]
    public async Task GetAllAsync_LoadsFromSource_OnCacheMiss()
    {
        var sources = new List<BrewerySource>
        {
            new() { Id = "1", Name = "Test Brewery", City = "Portland", BreweryType = "micro" }
        };
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        var result = await _repository.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("Test Brewery", result[0].Name);
        _loaderMock.Verify(l => l.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCached_OnCacheHit()
    {
        var sources = new List<BrewerySource>
        {
            new() { Id = "1", Name = "Cached", City = "Denver", BreweryType = "nano" }
        };
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        var first = await _repository.GetAllAsync();
        var second = await _repository.GetAllAsync();

        Assert.Equal(first.Count, second.Count);
        _loaderMock.Verify(l => l.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_MapsSourceToDomain()
    {
        var sources = new List<BrewerySource>
        {
            new()
            {
                Id = "xyz", Name = "Mapped", BreweryType = "brewpub",
                Address1 = "100 Main St", City = "Austin", StateProvince = "Texas",
                PostalCode = "73301", Country = "United States",
                Latitude = 30.27, Longitude = -97.74,
                Phone = "5121234567", WebsiteUrl = "https://mapped.com"
            }
        };
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        var result = await _repository.GetAllAsync();

        Assert.Equal("xyz", result[0].Id);
        Assert.Equal("Mapped", result[0].Name);
        Assert.Equal("100 Main St", result[0].Address);
        Assert.Equal(30.27, result[0].Latitude);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsBrewery()
    {
        var sources = new List<BrewerySource>
        {
            new() { Id = "abc-123", Name = "Find Me", City = "Seattle", BreweryType = "micro" },
            new() { Id = "def-456", Name = "Not Me", City = "Portland", BreweryType = "large" }
        };
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        var result = await _repository.GetByIdAsync("abc-123");

        Assert.NotNull(result);
        Assert.Equal("Find Me", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var sources = new List<BrewerySource>
        {
            new() { Id = "abc-123", Name = "Only One", City = "Boston", BreweryType = "micro" }
        };
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        var result = await _repository.GetByIdAsync("not-found");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_CaseInsensitive()
    {
        var sources = new List<BrewerySource>
        {
            new() { Id = "ABC-123", Name = "Case Test", City = "NYC", BreweryType = "micro" }
        };
        _loaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sources);

        var result = await _repository.GetByIdAsync("abc-123");

        Assert.NotNull(result);
    }
}

using ELFAssessment.API.Data;
using ELFAssessment.API.Models;
using ELFAssessment.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ELFAssessment.Tests;

public class DatabaseInitializerTests : IDisposable
{
    private readonly BreweryDbContext _db;

    public DatabaseInitializerTests()
    {
        var dbOptions = new DbContextOptionsBuilder<BreweryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new BreweryDbContext(dbOptions);
    }

    [Fact]
    public async Task InitializeAsync_EmptyDb_SeedsData()
    {
        var loaderMock = new Mock<IBrewerySourceLoader>();
        loaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BrewerySource>
            {
                new() { Id = "1", Name = "Seeded", City = "Portland", BreweryType = "micro", Address1 = "100 Main St" }
            });

        var initializer = new DatabaseInitializer(_db, loaderMock.Object, Mock.Of<ILogger<DatabaseInitializer>>());

        await initializer.InitializeAsync();

        Assert.Single(_db.Breweries);
        Assert.Equal("Seeded", _db.Breweries.First().Name);
        Assert.Equal("100 Main St", _db.Breweries.First().Address);
    }

    [Fact]
    public async Task InitializeAsync_NonEmptyDb_SkipsSeeding()
    {
        _db.Breweries.Add(new BreweryEntity
        {
            Id = "existing", Name = "Already Here", City = "Denver",
            StateProvince = "CO", PostalCode = "80201", Country = "US", BreweryType = "micro"
        });
        await _db.SaveChangesAsync();

        var loaderMock = new Mock<IBrewerySourceLoader>();
        var initializer = new DatabaseInitializer(_db, loaderMock.Object, Mock.Of<ILogger<DatabaseInitializer>>());

        await initializer.InitializeAsync();

        loaderMock.Verify(l => l.LoadAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(_db.Breweries);
    }

    [Fact]
    public async Task InitializeAsync_MapsAddressFields()
    {
        var loaderMock = new Mock<IBrewerySourceLoader>();
        loaderMock.Setup(l => l.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BrewerySource>
            {
                new()
                {
                    Id = "addr", Name = "Address Test", City = "Austin",
                    BreweryType = "brewpub",
                    Address1 = "Line 1", Address2 = "Line 2", Address3 = "Line 3"
                }
            });

        var initializer = new DatabaseInitializer(_db, loaderMock.Object, Mock.Of<ILogger<DatabaseInitializer>>());
        await initializer.InitializeAsync();

        var entity = _db.Breweries.First();
        Assert.Equal("Line 1, Line 2, Line 3", entity.Address);
    }

    public void Dispose() => _db.Dispose();
}

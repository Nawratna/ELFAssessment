using ELFAssessment.API.Models;
using ELFAssessment.API.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ELFAssessment.Tests;

public class BreweryServiceTests
{
    private readonly Mock<IBreweryRepository> _repoMock;
    private readonly BreweryService _service;
    private readonly List<Brewery> _testBreweries;

    public BreweryServiceTests()
    {
        _repoMock = new Mock<IBreweryRepository>();
        var logger = Mock.Of<ILogger<BreweryService>>();
        _service = new BreweryService(_repoMock.Object, logger);

        _testBreweries = new List<Brewery>
        {
            new() { Id = "1", Name = "Alpha Brewing", City = "Portland", StateProvince = "Oregon", Country = "United States", BreweryType = "micro", PostalCode = "97201", Latitude = 45.52, Longitude = -122.68 },
            new() { Id = "2", Name = "Beta Brewpub", City = "Austin", StateProvince = "Texas", Country = "United States", BreweryType = "brewpub", PostalCode = "73301", Latitude = 30.27, Longitude = -97.74 },
            new() { Id = "3", Name = "Charlie Craft", City = "Denver", StateProvince = "Colorado", Country = "United States", BreweryType = "micro", PostalCode = "80201", Latitude = 39.74, Longitude = -104.99 },
            new() { Id = "4", Name = "Delta Distillery", City = "Seattle", StateProvince = "Washington", Country = "United States", BreweryType = "large", PostalCode = "98101", Latitude = 47.61, Longitude = -122.33 },
            new() { Id = "5", Name = "Echo IPA", City = "Portland", StateProvince = "Oregon", Country = "United States", BreweryType = "nano", PostalCode = "97201", Latitude = null, Longitude = null },
        };

        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testBreweries.AsReadOnly());
    }

    // ── Search Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task GetBreweries_NoSearch_ReturnsAll()
    {
        var query = new BreweryQuery();
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
    }

    [Fact]
    public async Task GetBreweries_SearchByName_FiltersCorrectly()
    {
        var query = new BreweryQuery { Search = "Alpha" };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Single(result.Items);
        Assert.Equal("Alpha Brewing", result.Items[0].Name);
    }

    [Fact]
    public async Task GetBreweries_SearchByCity_FiltersCorrectly()
    {
        var query = new BreweryQuery { Search = "Portland" };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetBreweries_SearchByState_FiltersCorrectly()
    {
        var query = new BreweryQuery { Search = "Oregon" };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetBreweries_SearchByType_FiltersCorrectly()
    {
        var query = new BreweryQuery { Search = "micro" };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetBreweries_SearchByCountry_FiltersCorrectly()
    {
        var query = new BreweryQuery { Search = "United States" };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task GetBreweries_SearchByPostalCode_FiltersCorrectly()
    {
        var query = new BreweryQuery { Search = "97201" };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetBreweries_SearchCaseInsensitive()
    {
        var query = new BreweryQuery { Search = "alpha" };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetBreweries_SearchNoMatch_ReturnsEmpty()
    {
        var query = new BreweryQuery { Search = "ZZZZZZZ" };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    // ── Sort Tests ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetBreweries_SortByNameAsc()
    {
        var query = new BreweryQuery { SortBy = BrewerySortBy.Name, SortDirection = SortDirection.Asc };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal("Alpha Brewing", result.Items[0].Name);
        Assert.Equal("Echo IPA", result.Items[^1].Name);
    }

    [Fact]
    public async Task GetBreweries_SortByNameDesc()
    {
        var query = new BreweryQuery { SortBy = BrewerySortBy.Name, SortDirection = SortDirection.Desc };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal("Echo IPA", result.Items[0].Name);
        Assert.Equal("Alpha Brewing", result.Items[^1].Name);
    }

    [Fact]
    public async Task GetBreweries_SortByCityAsc()
    {
        var query = new BreweryQuery { SortBy = BrewerySortBy.City, SortDirection = SortDirection.Asc };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal("Austin", result.Items[0].City);
    }

    [Fact]
    public async Task GetBreweries_SortByCityDesc()
    {
        var query = new BreweryQuery { SortBy = BrewerySortBy.City, SortDirection = SortDirection.Desc };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal("Seattle", result.Items[0].City);
    }

    [Fact]
    public async Task GetBreweries_SortByDistanceAsc_ClosestFirst()
    {
        // Origin: Portland (45.52, -122.68) — Alpha and Echo are in Portland
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.Distance,
            SortDirection = SortDirection.Asc,
            Latitude = 45.52,
            Longitude = -122.68
        };

        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal("Alpha Brewing", result.Items[0].Name); // Portland, exact coords
        Assert.Equal("Delta Distillery", result.Items[1].Name); // Seattle, closest next
    }

    [Fact]
    public async Task GetBreweries_SortByDistance_NullCoords_PlacedLast()
    {
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.Distance,
            SortDirection = SortDirection.Asc,
            Latitude = 45.52,
            Longitude = -122.68
        };

        var result = await _service.GetBreweriesAsync(query);

        // Echo IPA has null coords, should be last
        Assert.Equal("Echo IPA", result.Items[^1].Name);
    }

    [Fact]
    public async Task GetBreweries_SortByDistanceWithoutCoords_DefaultsToNameSort()
    {
        var query = new BreweryQuery
        {
            SortBy = BrewerySortBy.Distance,
            SortDirection = SortDirection.Asc
            // No lat/lon provided – falls back to Name sort
        };

        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal("Alpha Brewing", result.Items[0].Name);
    }

    // ── Paging Tests ───────────────────────────────────────────────────

    [Fact]
    public async Task GetBreweries_Paging_Page1()
    {
        var query = new BreweryQuery { Page = 1, PageSize = 2 };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetBreweries_Paging_Page2()
    {
        var query = new BreweryQuery { Page = 2, PageSize = 2 };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetBreweries_Paging_LastPage()
    {
        var query = new BreweryQuery { Page = 3, PageSize = 2 };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetBreweries_Paging_BeyondLastPage_ReturnsEmpty()
    {
        var query = new BreweryQuery { Page = 10, PageSize = 2 };
        var result = await _service.GetBreweriesAsync(query);

        Assert.Empty(result.Items);
        Assert.Equal(5, result.TotalCount);
    }

    // ── GetById Tests ──────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Exists_ReturnsBrewery()
    {
        _repoMock.Setup(r => r.GetByIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testBreweries[0]);

        var result = await _service.GetByIdAsync("1");

        Assert.NotNull(result);
        Assert.Equal("Alpha Brewing", result.Name);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brewery?)null);

        var result = await _service.GetByIdAsync("nonexistent");

        Assert.Null(result);
    }

    // ── Autocomplete Tests ─────────────────────────────────────────────

    [Fact]
    public async Task Autocomplete_EmptyTerm_ReturnsEmpty()
    {
        var result = await _service.AutocompleteAsync("");
        Assert.Empty(result);
    }

    [Fact]
    public async Task Autocomplete_WhitespaceTerm_ReturnsEmpty()
    {
        var result = await _service.AutocompleteAsync("   ");
        Assert.Empty(result);
    }

    [Fact]
    public async Task Autocomplete_MatchingTerm_ReturnsNames()
    {
        var result = await _service.AutocompleteAsync("Brew");

        Assert.Equal(2, result.Count); // Alpha Brewing, Beta Brewpub
    }

    [Fact]
    public async Task Autocomplete_PrefixMatchesFirst()
    {
        var result = await _service.AutocompleteAsync("Alpha");

        Assert.Single(result);
        Assert.Equal("Alpha Brewing", result[0]);
    }

    [Fact]
    public async Task Autocomplete_RespectsLimit()
    {
        var result = await _service.AutocompleteAsync("a", limit: 1);

        Assert.Single(result);
    }

    [Fact]
    public async Task Autocomplete_CaseInsensitive()
    {
        var result = await _service.AutocompleteAsync("alpha");

        Assert.Single(result);
        Assert.Equal("Alpha Brewing", result[0]);
    }

    [Fact]
    public async Task Autocomplete_NoMatch_ReturnsEmpty()
    {
        var result = await _service.AutocompleteAsync("ZZZZZ");
        Assert.Empty(result);
    }
}

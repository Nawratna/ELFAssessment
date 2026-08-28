using ELFAssessment.API.Controllers.V1;
using ELFAssessment.API.Models;
using ELFAssessment.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace ELFAssessment.Tests;

public class BreweriesControllerTests
{
    private readonly Mock<IBreweryService> _serviceMock;
    private readonly BreweriesController _controller;

    public BreweriesControllerTests()
    {
        _serviceMock = new Mock<IBreweryService>();
        var logger = Mock.Of<ILogger<BreweriesController>>();
        _controller = new BreweriesController(_serviceMock.Object, logger);
    }

    // ── GetBreweries ───────────────────────────────────────────────────

    [Fact]
    public async Task GetBreweries_ReturnsOkWithPagedResult()
    {
        var paged = new PagedResult<Brewery>
        {
            Items = new List<Brewery> { new() { Id = "1", Name = "Test" } },
            TotalCount = 1,
            Page = 1,
            PageSize = 50
        };
        _serviceMock.Setup(s => s.GetBreweriesAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _controller.GetBreweries(null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<PagedResult<Brewery>>(okResult.Value);
        Assert.Single(returned.Items);
    }

    [Fact]
    public async Task GetBreweries_WithSearch_PassesQueryToService()
    {
        BreweryQuery? capturedQuery = null;
        _serviceMock.Setup(s => s.GetBreweriesAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .Callback<BreweryQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new PagedResult<Brewery>());

        await _controller.GetBreweries("micro", sortBy: BrewerySortBy.City, sortDirection: SortDirection.Desc, page: 2, pageSize: 25);

        Assert.NotNull(capturedQuery);
        Assert.Equal("micro", capturedQuery!.Search);
        Assert.Equal(BrewerySortBy.City, capturedQuery.SortBy);
        Assert.Equal(SortDirection.Desc, capturedQuery.SortDirection);
        Assert.Equal(2, capturedQuery.Page);
        Assert.Equal(25, capturedQuery.PageSize);
    }

    [Fact]
    public async Task GetBreweries_SortByDistanceWithoutCoords_ReturnsBadRequest()
    {
        var result = await _controller.GetBreweries(null, sortBy: BrewerySortBy.Distance);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBreweries_SortByDistanceWithCoords_ReturnsOk()
    {
        _serviceMock.Setup(s => s.GetBreweriesAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Brewery>());

        var result = await _controller.GetBreweries(null, sortBy: BrewerySortBy.Distance, latitude: 45.0, longitude: -122.0);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetBreweries_PageSizeClampedTo200()
    {
        BreweryQuery? capturedQuery = null;
        _serviceMock.Setup(s => s.GetBreweriesAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .Callback<BreweryQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new PagedResult<Brewery>());

        await _controller.GetBreweries(null, pageSize: 500);

        Assert.Equal(200, capturedQuery!.PageSize);
    }

    [Fact]
    public async Task GetBreweries_NegativePage_ClampedTo1()
    {
        BreweryQuery? capturedQuery = null;
        _serviceMock.Setup(s => s.GetBreweriesAsync(It.IsAny<BreweryQuery>(), It.IsAny<CancellationToken>()))
            .Callback<BreweryQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new PagedResult<Brewery>());

        await _controller.GetBreweries(null, page: -5);

        Assert.Equal(1, capturedQuery!.Page);
    }

    // ── GetBrewery ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetBrewery_Exists_ReturnsOk()
    {
        _serviceMock.Setup(s => s.GetByIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Brewery { Id = "1", Name = "Found" });

        var result = await _controller.GetBrewery("1", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var brewery = Assert.IsType<Brewery>(okResult.Value);
        Assert.Equal("Found", brewery.Name);
    }

    [Fact]
    public async Task GetBrewery_NotFound_Returns404()
    {
        _serviceMock.Setup(s => s.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brewery?)null);

        var result = await _controller.GetBrewery("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Autocomplete ───────────────────────────────────────────────────

    [Fact]
    public async Task Autocomplete_EmptyTerm_ReturnsEmptyOk()
    {
        var result = await _controller.Autocomplete("");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsType<string[]>(okResult.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task Autocomplete_ValidTerm_ReturnsNames()
    {
        _serviceMock.Setup(s => s.AutocompleteAsync("Brew", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Brewery A", "Brewery B" });

        var result = await _controller.Autocomplete("Brew");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<string>>(okResult.Value);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task Autocomplete_LimitClampedTo50()
    {
        _serviceMock.Setup(s => s.AutocompleteAsync("A", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        await _controller.Autocomplete("A", limit: 100);

        _serviceMock.Verify(s => s.AutocompleteAsync("A", 50, It.IsAny<CancellationToken>()), Times.Once);
    }
}

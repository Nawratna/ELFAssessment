using Asp.Versioning;
using ELFAssessment.API.Models;
using ELFAssessment.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ELFAssessment.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class BreweriesController : ControllerBase
{
    private readonly IBreweryService _service;
    private readonly ILogger<BreweriesController> _logger;

    public BreweriesController(IBreweryService service, ILogger<BreweriesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>List breweries with search, sort and paging.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<Brewery>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBreweries(
        [FromQuery] string? search,
        [FromQuery] BrewerySortBy sortBy = BrewerySortBy.Name,
        [FromQuery] SortDirection sortDirection = SortDirection.Asc,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (sortBy == BrewerySortBy.Distance && (!latitude.HasValue || !longitude.HasValue))
            return BadRequest(new { error = "latitude and longitude are required when sorting by distance." });

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 200) pageSize = 200;

        var query = new BreweryQuery
        {
            Search = search,
            SortBy = sortBy,
            SortDirection = sortDirection,
            Latitude = latitude,
            Longitude = longitude,
            Page = page,
            PageSize = pageSize
        };

        _logger.LogInformation("GET breweries – search={Search}, sortBy={SortBy}, page={Page}", search, sortBy, page);
        var result = await _service.GetBreweriesAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Get a single brewery by ID.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Brewery), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBrewery(string id, CancellationToken cancellationToken)
    {
        var brewery = await _service.GetByIdAsync(id, cancellationToken);
        return brewery is null ? NotFound() : Ok(brewery);
    }

    /// <summary>Autocomplete brewery names.</summary>
    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Autocomplete(
        [FromQuery] string term,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Ok(Array.Empty<string>());

        if (limit < 1) limit = 1;
        if (limit > 50) limit = 50;

        var results = await _service.AutocompleteAsync(term, limit, cancellationToken);
        return Ok(results);
    }
}

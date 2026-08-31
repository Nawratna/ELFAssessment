using ELFAssessment.API.Models;

namespace ELFAssessment.API.Services;

/// <summary>
/// Core business logic: applies search filtering, sorting (name/city/distance), pagination,
/// and autocomplete over the cached brewery dataset from <see cref="IBreweryRepository"/>.
/// </summary>
public sealed class BreweryService : IBreweryService
{
    private readonly IBreweryRepository _repository;
    private readonly ILogger<BreweryService> _logger;

    public BreweryService(IBreweryRepository repository, ILogger<BreweryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>Loads all breweries, applies search → sort → pagination, and returns a paged result.</summary>
    public async Task<PagedResult<Brewery>> GetBreweriesAsync(BreweryQuery query, CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        var filtered = Filter(all, query.Search);
        var sorted = Sort(filtered, query);
        var totalCount = sorted.Count;
        var paged = sorted.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList();

        _logger.LogInformation("Query returned {Count}/{Total} breweries (page {Page})", paged.Count, totalCount, query.Page);

        return new PagedResult<Brewery>
        {
            Items = paged,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    /// <summary>Retrieves a single brewery by its unique ID.</summary>
    public async Task<Brewery?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>Returns distinct brewery names containing the term, with prefix matches ranked first.</summary>
    public async Task<IReadOnlyList<string>> AutocompleteAsync(string term, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(term))
            return [];

        var all = await _repository.GetAllAsync(cancellationToken);

        return all
            .Where(b => b.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            .OrderBy(b => !(b.Name?.StartsWith(term, StringComparison.OrdinalIgnoreCase) ?? false))
            .ThenBy(b => b.Name)
            .Select(b => b.Name)
            .Distinct()
            .Take(limit)
            .ToList();
    }

    /// <summary>Case-insensitive search across name, city, state, country, type, and postal code.</summary>
    private static List<Brewery> Filter(IReadOnlyList<Brewery> breweries, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return breweries.ToList();

        return breweries.Where(b =>
            (b.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (b.City?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (b.StateProvince?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (b.Country?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (b.BreweryType?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (b.PostalCode?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();
    }

    /// <summary>Sorts breweries by the requested field and direction. Distance uses haversine formula.</summary>
    private static List<Brewery> Sort(List<Brewery> breweries, BreweryQuery query)
    {
        IOrderedEnumerable<Brewery> ordered = query.SortBy switch
        {
            BrewerySortBy.City => query.SortDirection == SortDirection.Asc
                ? breweries.OrderBy(b => b.City, StringComparer.OrdinalIgnoreCase)
                : breweries.OrderByDescending(b => b.City, StringComparer.OrdinalIgnoreCase),

            BrewerySortBy.Distance when query.Latitude.HasValue && query.Longitude.HasValue =>
                query.SortDirection == SortDirection.Asc
                    ? breweries.OrderBy(b => DistanceFrom(b, query.Latitude.Value, query.Longitude.Value))
                    : breweries.OrderByDescending(b => DistanceFrom(b, query.Latitude.Value, query.Longitude.Value)),

            // Default: sort by Name
            _ => query.SortDirection == SortDirection.Asc
                ? breweries.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                : breweries.OrderByDescending(b => b.Name, StringComparer.OrdinalIgnoreCase),
        };

        return ordered.ToList();
    }

    /// <summary>Calculates distance from origin to brewery. Returns double.MaxValue if coordinates are missing.</summary>
    private static double DistanceFrom(Brewery b, double lat, double lon)
    {
        if (!b.Latitude.HasValue || !b.Longitude.HasValue)
            return double.MaxValue;

        return GeoDistance.Calculate(lat, lon, b.Latitude.Value, b.Longitude.Value);
    }
}

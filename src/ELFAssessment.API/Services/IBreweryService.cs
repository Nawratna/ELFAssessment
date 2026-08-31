using ELFAssessment.API.Models;

namespace ELFAssessment.API.Services;

/// <summary>Data access abstraction for brewery storage. Implemented by InMemoryBreweryRepository and SqliteBreweryRepository.</summary>
public interface IBreweryRepository
{
    /// <summary>Returns all breweries from the underlying store (may be cached).</summary>
    Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a single brewery by its unique ID, or null if not found.</summary>
    Task<Brewery?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>Business logic layer for brewery operations: search, sort, pagination, and autocomplete.</summary>
public interface IBreweryService
{
    /// <summary>Returns a filtered, sorted, and paginated list of breweries based on the query parameters.</summary>
    Task<PagedResult<Brewery>> GetBreweriesAsync(BreweryQuery query, CancellationToken cancellationToken = default);

    /// <summary>Returns a single brewery by ID, or null if not found.</summary>
    Task<Brewery?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Returns brewery name suggestions matching the given prefix/term, ordered by relevance.</summary>
    Task<IReadOnlyList<string>> AutocompleteAsync(string term, int limit = 10, CancellationToken cancellationToken = default);
}

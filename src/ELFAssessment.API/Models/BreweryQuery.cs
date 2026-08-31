namespace ELFAssessment.API.Models;

/// <summary>Query parameters for the brewery list endpoint. Supports search, sort, and pagination.</summary>
public sealed class BreweryQuery
{
    /// <summary>Case-insensitive search term matched against name, city, state, country, type, and postal code.</summary>
    public string? Search { get; set; }

    /// <summary>Field to sort results by. Default: Name.</summary>
    public BrewerySortBy SortBy { get; set; } = BrewerySortBy.Name;

    /// <summary>Sort direction. Default: Ascending.</summary>
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;

    /// <summary>Origin latitude for distance sorting. Required when SortBy is Distance.</summary>
    public double? Latitude { get; set; }

    /// <summary>Origin longitude for distance sorting. Required when SortBy is Distance.</summary>
    public double? Longitude { get; set; }

    /// <summary>1-based page number. Default: 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of items per page (max 200). Default: 50.</summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>Supported sort fields for brewery listings.</summary>
public enum BrewerySortBy
{
    /// <summary>Sort alphabetically by brewery name.</summary>
    Name,
    /// <summary>Sort alphabetically by city.</summary>
    City,
    /// <summary>Sort by haversine distance from the provided latitude/longitude origin.</summary>
    Distance
}

/// <summary>Sort direction.</summary>
public enum SortDirection
{
    /// <summary>Ascending (A→Z, nearest→farthest).</summary>
    Asc,
    /// <summary>Descending (Z→A, farthest→nearest).</summary>
    Desc
}

/// <summary>Paginated result wrapper with total count metadata.</summary>
public sealed class PagedResult<T>
{
    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>Total number of items matching the query (across all pages).</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Requested page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of pages (computed from TotalCount and PageSize).</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

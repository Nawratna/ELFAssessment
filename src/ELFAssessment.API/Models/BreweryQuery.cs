namespace ELFAssessment.API.Models;

public sealed class BreweryQuery
{
    public string? Search { get; set; }
    public BrewerySortBy SortBy { get; set; } = BrewerySortBy.Name;
    public SortDirection SortDirection { get; set; } = SortDirection.Asc;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public enum BrewerySortBy
{
    Name,
    City,
    Distance
}

public enum SortDirection
{
    Asc,
    Desc
}

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

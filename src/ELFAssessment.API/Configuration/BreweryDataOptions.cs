namespace ELFAssessment.API.Configuration;

/// <summary>Configuration for the brewery data source, caching, and storage provider. Bound to the "BreweryData" section in appsettings.json.</summary>
public sealed class BreweryDataOptions
{
    public const string SectionName = "BreweryData";

    /// <summary>Base URL of the Open Brewery DB API.</summary>
    public string SourceApiUrl { get; set; } = "https://api.openbrewerydb.org/v1/breweries";

    /// <summary>How long to cache brewery data before refreshing from the source. Default: 10 minutes.</summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Storage backend: "InMemory" (default) or "Sqlite" for EF Core persistence.</summary>
    public string StorageProvider { get; set; } = "InMemory";

    /// <summary>SQLite connection string. Only used when StorageProvider is "Sqlite".</summary>
    public string ConnectionString { get; set; } = "Data Source=breweries.db";

    /// <summary>Number of breweries to fetch per API page (max allowed by Open Brewery DB: 200).</summary>
    public int SourcePageSize { get; set; } = 200;
}

/// <summary>Configuration for API key authentication. Bound to the "ApiKey" section in appsettings.json.</summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    /// <summary>HTTP header name that carries the API key. Default: X-Api-Key.</summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>Expected API key value. If empty, authentication is skipped (dev convenience).</summary>
    public string Value { get; set; } = string.Empty;
}

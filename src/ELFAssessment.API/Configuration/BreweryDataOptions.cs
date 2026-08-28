namespace ELFAssessment.API.Configuration;

public sealed class BreweryDataOptions
{
    public const string SectionName = "BreweryData";

    public string SourceApiUrl { get; set; } = "https://api.openbrewerydb.org/v1/breweries";
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(10);
    public string StorageProvider { get; set; } = "InMemory"; // InMemory or Sqlite
    public string ConnectionString { get; set; } = "Data Source=breweries.db";
    public int SourcePageSize { get; set; } = 200; // max per_page from API
}

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    public string HeaderName { get; set; } = "X-Api-Key";
    public string Value { get; set; } = string.Empty;
}

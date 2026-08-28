using System.Text.Json;
using ELFAssessment.API.Configuration;
using ELFAssessment.API.Models;
using Microsoft.Extensions.Options;

namespace ELFAssessment.API.Services;

/// <summary>Fetches brewery data from the Open Brewery DB API, paginating through all results.</summary>
public sealed class OpenBreweryDbLoader : IBrewerySourceLoader
{
    private readonly HttpClient _httpClient;
    private readonly BreweryDataOptions _options;
    private readonly ILogger<OpenBreweryDbLoader> _logger;

    public OpenBreweryDbLoader(HttpClient httpClient, IOptions<BreweryDataOptions> options, ILogger<OpenBreweryDbLoader> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BrewerySource>> LoadAsync(CancellationToken cancellationToken = default)
    {
        var all = new List<BrewerySource>();
        int page = 1;

        while (true)
        {
            var url = $"{_options.SourceApiUrl}?page={page}&per_page={_options.SourcePageSize}";
            _logger.LogInformation("Fetching breweries from {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var batch = JsonSerializer.Deserialize<List<BrewerySource>>(json) ?? [];

            if (batch.Count == 0)
                break;

            all.AddRange(batch);
            _logger.LogInformation("Loaded {Count} breweries (page {Page})", batch.Count, page);

            if (batch.Count < _options.SourcePageSize)
                break;

            page++;
        }

        _logger.LogInformation("Total breweries loaded from API: {Total}", all.Count);
        return all;
    }
}

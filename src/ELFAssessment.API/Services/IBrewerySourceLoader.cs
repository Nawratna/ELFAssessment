using ELFAssessment.API.Models;

namespace ELFAssessment.API.Services;

/// <summary>Abstraction for loading raw brewery data from the external source (Open Brewery DB API).</summary>
public interface IBrewerySourceLoader
{
    /// <summary>Fetches all brewery records from the source, paginating through all available pages.</summary>
    Task<IReadOnlyList<BrewerySource>> LoadAsync(CancellationToken cancellationToken = default);
}

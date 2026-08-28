using ELFAssessment.API.Models;

namespace ELFAssessment.API.Services;

public interface IBreweryRepository
{
    Task<IReadOnlyList<Brewery>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Brewery?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}

public interface IBreweryService
{
    Task<PagedResult<Brewery>> GetBreweriesAsync(BreweryQuery query, CancellationToken cancellationToken = default);
    Task<Brewery?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> AutocompleteAsync(string term, int limit = 10, CancellationToken cancellationToken = default);
}

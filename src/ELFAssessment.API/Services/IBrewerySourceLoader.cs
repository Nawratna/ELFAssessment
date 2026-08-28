using ELFAssessment.API.Models;

namespace ELFAssessment.API.Services;

public interface IBrewerySourceLoader
{
    Task<IReadOnlyList<BrewerySource>> LoadAsync(CancellationToken cancellationToken = default);
}

using ELFAssessment.API.Models;

namespace ELFAssessment.API.Services;

public static class BreweryMapper
{
    public static Brewery ToDomain(BrewerySource source)
    {
        return new Brewery
        {
            Id = source.Id,
            Name = source.Name,
            BreweryType = source.BreweryType,
            Address = BuildAddress(source.Address1, source.Address2, source.Address3),
            City = source.City,
            StateProvince = source.StateProvince,
            PostalCode = source.PostalCode,
            Country = source.Country,
            Longitude = source.Longitude,
            Latitude = source.Latitude,
            Phone = source.Phone,
            WebsiteUrl = source.WebsiteUrl
        };
    }

    private static string? BuildAddress(string? a1, string? a2, string? a3)
    {
        var parts = new[] { a1, a2, a3 }.Where(p => !string.IsNullOrWhiteSpace(p));
        var joined = string.Join(", ", parts);
        return string.IsNullOrEmpty(joined) ? null : joined;
    }
}

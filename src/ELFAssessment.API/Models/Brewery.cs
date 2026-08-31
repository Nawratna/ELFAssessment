namespace ELFAssessment.API.Models;

/// <summary>
/// Generic API-facing domain model.
/// Mapped from <see cref="BrewerySource"/> via <see cref="Services.BreweryMapper"/>.
/// Decouples the public API contract from the external Open Brewery DB schema.
/// </summary>
public sealed class Brewery
{
    /// <summary>Unique brewery identifier (GUID from Open Brewery DB).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Brewery display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Type of brewery: micro, nano, regional, brewpub, large, planning, contract, proprietor, closed.</summary>
    public string BreweryType { get; set; } = string.Empty;

    /// <summary>Concatenated street address (address lines 1–3 joined with commas). Null if no address data.</summary>
    public string? Address { get; set; }

    /// <summary>City where the brewery is located.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>State or province name.</summary>
    public string StateProvince { get; set; } = string.Empty;

    /// <summary>Postal / ZIP code.</summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>Country name.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Longitude coordinate. Null if not available.</summary>
    public double? Longitude { get; set; }

    /// <summary>Latitude coordinate. Null if not available.</summary>
    public double? Latitude { get; set; }

    /// <summary>Contact phone number. Null if not available.</summary>
    public string? Phone { get; set; }

    /// <summary>Brewery website URL. Null if not available.</summary>
    public string? WebsiteUrl { get; set; }
}

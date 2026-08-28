using System.ComponentModel.DataAnnotations;

namespace ELFAssessment.API.Data;

public sealed class BreweryEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BreweryType { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string City { get; set; } = string.Empty;
    public string StateProvince { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public string? Phone { get; set; }
    public string? WebsiteUrl { get; set; }
}

using System.Text.Json.Serialization;

namespace ELFAssessment.API.Models;

/// <summary>Raw shape from the Open Brewery DB API.</summary>
public sealed class BrewerySource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("brewery_type")]
    public string BreweryType { get; set; } = string.Empty;

    [JsonPropertyName("address_1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address_2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("address_3")]
    public string? Address3 { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state_province")]
    public string StateProvince { get; set; } = string.Empty;

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("street")]
    public string? Street { get; set; }
}

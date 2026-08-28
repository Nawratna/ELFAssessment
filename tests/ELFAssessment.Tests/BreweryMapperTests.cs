using ELFAssessment.API.Models;
using ELFAssessment.API.Services;

namespace ELFAssessment.Tests;

public class BreweryMapperTests
{
    [Fact]
    public void ToDomain_MapsAllFields()
    {
        var source = new BrewerySource
        {
            Id = "abc-123",
            Name = "Test Brewery",
            BreweryType = "micro",
            Address1 = "123 Main St",
            Address2 = "Suite 100",
            Address3 = null,
            City = "Portland",
            StateProvince = "Oregon",
            PostalCode = "97201",
            Country = "United States",
            Longitude = -122.68,
            Latitude = 45.52,
            Phone = "5031234567",
            WebsiteUrl = "https://test.com"
        };

        var result = BreweryMapper.ToDomain(source);

        Assert.Equal("abc-123", result.Id);
        Assert.Equal("Test Brewery", result.Name);
        Assert.Equal("micro", result.BreweryType);
        Assert.Equal("123 Main St, Suite 100", result.Address);
        Assert.Equal("Portland", result.City);
        Assert.Equal("Oregon", result.StateProvince);
        Assert.Equal("97201", result.PostalCode);
        Assert.Equal("United States", result.Country);
        Assert.Equal(-122.68, result.Longitude);
        Assert.Equal(45.52, result.Latitude);
        Assert.Equal("5031234567", result.Phone);
        Assert.Equal("https://test.com", result.WebsiteUrl);
    }

    [Fact]
    public void ToDomain_AllAddressPartsNull_ReturnsNullAddress()
    {
        var source = new BrewerySource
        {
            Id = "1",
            Name = "No Address Brewery",
            Address1 = null,
            Address2 = null,
            Address3 = null,
            City = "Portland"
        };

        var result = BreweryMapper.ToDomain(source);

        Assert.Null(result.Address);
    }

    [Fact]
    public void ToDomain_OnlyAddress1_ReturnsSingleAddress()
    {
        var source = new BrewerySource
        {
            Id = "2",
            Name = "Simple",
            Address1 = "123 Main St",
            Address2 = null,
            Address3 = null,
            City = "Denver"
        };

        var result = BreweryMapper.ToDomain(source);

        Assert.Equal("123 Main St", result.Address);
    }

    [Fact]
    public void ToDomain_AllThreeAddressParts_ConcatenatesWithComma()
    {
        var source = new BrewerySource
        {
            Id = "3",
            Name = "Full Address",
            Address1 = "Line 1",
            Address2 = "Line 2",
            Address3 = "Line 3",
            City = "Austin"
        };

        var result = BreweryMapper.ToDomain(source);

        Assert.Equal("Line 1, Line 2, Line 3", result.Address);
    }

    [Fact]
    public void ToDomain_NullCoordinates_PreservedAsNull()
    {
        var source = new BrewerySource
        {
            Id = "4",
            Name = "No Coords",
            Longitude = null,
            Latitude = null,
            City = "Seattle"
        };

        var result = BreweryMapper.ToDomain(source);

        Assert.Null(result.Longitude);
        Assert.Null(result.Latitude);
    }
}

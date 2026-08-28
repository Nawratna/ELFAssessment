using ELFAssessment.API.Models;

namespace ELFAssessment.Tests;

public class BreweryQueryTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var query = new BreweryQuery();

        Assert.Null(query.Search);
        Assert.Equal(BrewerySortBy.Name, query.SortBy);
        Assert.Equal(SortDirection.Asc, query.SortDirection);
        Assert.Null(query.Latitude);
        Assert.Null(query.Longitude);
        Assert.Equal(1, query.Page);
        Assert.Equal(50, query.PageSize);
    }

    [Fact]
    public void BrewerySortBy_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(BrewerySortBy), BrewerySortBy.Name));
        Assert.True(Enum.IsDefined(typeof(BrewerySortBy), BrewerySortBy.City));
        Assert.True(Enum.IsDefined(typeof(BrewerySortBy), BrewerySortBy.Distance));
    }

    [Fact]
    public void SortDirection_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(SortDirection), SortDirection.Asc));
        Assert.True(Enum.IsDefined(typeof(SortDirection), SortDirection.Desc));
    }
}

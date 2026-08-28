using ELFAssessment.API.Models;

namespace ELFAssessment.Tests;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_ExactDivision()
    {
        var result = new PagedResult<Brewery> { TotalCount = 100, PageSize = 50 };
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public void TotalPages_WithRemainder_RoundsUp()
    {
        var result = new PagedResult<Brewery> { TotalCount = 101, PageSize = 50 };
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void TotalPages_SingleItem()
    {
        var result = new PagedResult<Brewery> { TotalCount = 1, PageSize = 50 };
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public void TotalPages_Empty()
    {
        var result = new PagedResult<Brewery> { TotalCount = 0, PageSize = 50 };
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void TotalPages_PageSizeZero_ReturnsZero()
    {
        var result = new PagedResult<Brewery> { TotalCount = 10, PageSize = 0 };
        Assert.Equal(0, result.TotalPages);
    }
}

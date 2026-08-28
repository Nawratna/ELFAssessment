using ELFAssessment.API.Services;

namespace ELFAssessment.Tests;

public class GeoDistanceTests
{
    [Fact]
    public void Calculate_SamePoint_ReturnsZero()
    {
        var distance = GeoDistance.Calculate(45.0, -122.0, 45.0, -122.0);
        Assert.Equal(0.0, distance, precision: 5);
    }

    [Fact]
    public void Calculate_KnownDistance_ReturnsApproxCorrect()
    {
        // Portland (45.52, -122.68) to Seattle (47.61, -122.33) ≈ 233 km
        var distance = GeoDistance.Calculate(45.52, -122.68, 47.61, -122.33);
        Assert.InRange(distance, 230, 240);
    }

    [Fact]
    public void Calculate_Antipodal_ReturnsApproxHalfCircumference()
    {
        // North Pole to South Pole ≈ 20015 km
        var distance = GeoDistance.Calculate(90, 0, -90, 0);
        Assert.InRange(distance, 20000, 20100);
    }

    [Fact]
    public void Calculate_IsSymmetric()
    {
        var d1 = GeoDistance.Calculate(40.0, -74.0, 51.5, -0.12);
        var d2 = GeoDistance.Calculate(51.5, -0.12, 40.0, -74.0);
        Assert.Equal(d1, d2, precision: 5);
    }

    [Fact]
    public void Calculate_ShortDistance_ReturnsSmallValue()
    {
        // Two points very close together
        var distance = GeoDistance.Calculate(45.520, -122.680, 45.521, -122.681);
        Assert.True(distance < 1.0); // Less than 1 km
    }
}

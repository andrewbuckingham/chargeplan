using System.Diagnostics;
using MathNet.Numerics.Interpolation;

namespace ChargePlan.Service.UnitTests;

public class UnitTest1
{
    // [Theory]
    // [InlineData(
    //     new float[] { 0.3f, 1.0f, 0.3f },
    //     new float[] { })]
    // public void Test1()
    // {
    //     var calc = new Calculator();

    //     DateTime datum = DateTime.Today;

    //     StorageProfile storageProfile = new() { CapacityKilowatts = 4.8f };

    //     DemandProfile demandProfile = new();
    // }

    [Fact]
    public void ShiftableDemand_ProducesInterpolation()
    {
        var dehumidifiers = new ShiftableDemandValue[]
        {
            new (TimeSpan.FromHours(0.0), 1.0f),
            new (TimeSpan.FromHours(1.0), 0.0f)
        };

        var datum = DateTime.Today;

        var demand = new ShiftableDemand() { Values = dehumidifiers.ToList() }
            .AsDemandProfile(datum.AddHours(1))
            .AsSpline(StepInterpolation.Interpolate);

        var beforePeriod = demand.Interpolate(datum.AddHours(0.0).AsTotalHours());
        var duringPeriod = demand.Interpolate(datum.AddHours(1.9).AsTotalHours());
        var afterPeriod = demand.Interpolate(datum.AddHours(4).AsTotalHours());

        Assert.Equal(0.0, beforePeriod, 1);
        Assert.Equal(1.0, duringPeriod, 1);
        Assert.Equal(0.0, afterPeriod, 1);
    }
}
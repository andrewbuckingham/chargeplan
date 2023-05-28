namespace ChargePlan.Domain;

public interface IInterpolationFactory
{
    IInterpolation InterpolateBaseload(IEnumerable<double> xValues, IEnumerable<double> yValues);
    IInterpolation InterpolateShiftableDemand(IEnumerable<double> xValues, IEnumerable<double> yValues);
    IInterpolation InterpolateGeneration(IEnumerable<double> xValues, IEnumerable<double> yValues);
    IInterpolation InterpolateCharging(IEnumerable<double> xValues, IEnumerable<double> yValues);
    IInterpolation InterpolatePricing(IEnumerable<double> xValues, IEnumerable<double> yValues);
    IInterpolation InterpolateExport(IEnumerable<double> xValues, IEnumerable<double> yValues);
}
using System.Diagnostics;
using System.Linq;
using MathNet.Numerics.Interpolation;

namespace ChargePlan.Service;

public record Algorithm(
    StorageProfile StorageProfile,
    DemandProfile DemandProfile,
    GenerationProfile GenerationProfile,
    ChargeProfile ChargeProfile,
    PricingProfile PricingProfile,
    CurrentState CurrentState,
    IEnumerable<ShiftableDemand> ShiftableDemands)
{
    /// <summary>
    /// Iterate differing charge energies to arrive at the optimal given the predicted generation and demand.
    /// </summary>
    public Decision DecideStrategy()
    {
        var chargeRates = Enumerable
            .Range(1, 100)
            .Select(percent => (float)percent * StorageProfile.MaxChargeKilowatts / 100.0f);

        DateTime fromDate = DemandProfile.Values.Select(f => f.DateTime).Min();
        DateTime toDate = DemandProfile.Values.Select(f => f.DateTime).Max();

        // Establish what's best for the main demand profile.
        var results = chargeRates.Select(chargeLimit => new Calculator().Calculate(
                StorageProfile,
                new[] { DemandProfile }, // Don't add the shiftable demand yet
                GenerationProfile,
                ChargeProfile,
                PricingProfile,
                CurrentState,
                chargeLimit
            ))
            .OrderBy(f => f.TotalCost);

        var resultWithOptimalChargeRate = results.First();

        // Iterate through options for shiftable demand.
        // Fit the largest demand in first, and then iteratively the smaller ones.
        var orderedShiftableDemands = ShiftableDemands
            .OrderByDescending(demand => demand
                .AsDemandProfile(fromDate)
                .AsSpline(StepInterpolation.Interpolate)
                .Integrate(fromDate.AsTotalHours(), toDate.AsTotalHours()))
            .ToArray();

        var shiftByTimespans = Enumerable.Range(0, (int)(toDate - fromDate).TotalHours).Select(f => TimeSpan.FromHours(f));
        var shiftableDemandsAsProfiles = orderedShiftableDemands
            .Select(shiftableDemand => shiftByTimespans
                .Select(ts => shiftableDemand.AsDemandProfile(fromDate.Add(ts))) // Apply the profile at each trial hour
                .Where(f => f.Values.Select(g => g.DateTime).Max() < toDate)); // Don't allow to overrun main calculation period

        //var crossProductOfShiftableDemands = shiftableDemandsAsProfiles.;

        return resultWithOptimalChargeRate;
    }
}
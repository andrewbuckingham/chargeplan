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
        DateTime fromDate = DemandProfile.Values.Select(f => f.DateTime).Min();
        DateTime toDate = DemandProfile.Values.Select(f => f.DateTime).Max();

        // Iterate through options for shiftable demand.
        // Fit the largest demand in first, and then iteratively the smaller ones.
        var orderedShiftableDemands = ShiftableDemands
            .OrderByDescending(demand => demand
                .AsDemandProfile(fromDate)
                .AsSpline(StepInterpolation.Interpolate)
                .Integrate(fromDate.AsTotalHours(), toDate.AsTotalHours()))
            .ToArray();

        var shiftByTimespans = CreateTrialTimespans(fromDate, toDate);
        var shiftableDemandsAsProfiles = orderedShiftableDemands
            .Select(shiftableDemand =>
            (
                Name: shiftableDemand.Name,
                Trials: shiftByTimespans
                    .Select(ts => (ShiftedBy: ts, Demand: shiftableDemand.AsDemandProfile(fromDate.Add(ts)))) // Apply the profile at each trial hour
                    .Where(f => f.Demand.Values.Select(g => g.DateTime).Max() < toDate) // Don't allow to overrun main calculation period
                    .Where(f => f.Demand.Values.Select(g => g.DateTime.TimeOfDay).Min() >= shiftableDemand.Earliest.ToTimeSpan())
                    .Where(f => f.Demand.Values.Select(g => g.DateTime.TimeOfDay).Max() <= shiftableDemand.Latest.ToTimeSpan())
            ));

        Decision decision = IterateChargeRates(Enumerable.Empty<DemandProfile>());

        List<(TimeSpan ShiftedBy, DemandProfile DemandProfile, Decision Decision)> completed = new();
        foreach (var s in shiftableDemandsAsProfiles)
        {
            var optimal = s.Trials
                .Select(t => ((t.ShiftedBy, t.Demand, Decision: IterateChargeRates(completed.Select(f => f.DemandProfile).Concat(new[] { t.Demand })))))
                .ToArray()
                .OrderBy(f => f.Decision.TotalCost)
                .First();

            completed.Add((optimal.ShiftedBy, optimal.Demand, optimal.Decision));

            decision = completed.Last().Decision;
            Debug.WriteLine($"Charge rate: {decision.RecommendedChargeRateLimit} Undercharge: {decision.UnderchargeEnergy} Overcharge: {decision.OverchargeEnergy} Cost: £{decision.TotalCost.ToString("F2")} {s.Name}: {completed.Last().ShiftedBy}");
        }

        return decision;
    }

    private IEnumerable<TimeSpan> CreateTrialTimespans(DateTime fromDate, DateTime toDate)
    {
//      return Enumerable.Range(0, (int)(toDate - fromDate).TotalHours).Select(f => TimeSpan.FromHours(f));
        TimeSpan ts = TimeSpan.Zero;

        while (fromDate + ts < toDate)
        {
            yield return ts;
            ts += TimeSpan.FromMinutes(30);
        }
    }

    private Decision IterateChargeRates(IEnumerable<DemandProfile> shiftableDemandsAsProfiles)
    {
        var chargeRates = Enumerable
            .Range(1, 100)
            .Select(percent => (float)percent * StorageProfile.MaxChargeKilowatts / 100.0f);

        var results = chargeRates.Select(chargeLimit => new Calculator().Calculate(
                StorageProfile,
                DemandProfile,
                shiftableDemandsAsProfiles,
                GenerationProfile,
                ChargeProfile,
                PricingProfile,
                CurrentState,
                chargeLimit
            ))
            .ToArray()
            .OrderBy(f => f.TotalCost);

        var resultWithOptimalChargeRate = results.First();

        return resultWithOptimalChargeRate;
    }
}
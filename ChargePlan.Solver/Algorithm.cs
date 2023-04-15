using System.Diagnostics;
using System.Linq;
using MathNet.Numerics.Interpolation;

public record Algorithm(
    IPlant PlantTemplate,
    IDemandProfile DemandProfile,
    IGenerationProfile GenerationProfile,
    IChargeProfile ChargeProfile,
    IPricingProfile PricingProfile,
    PlantState InitialState,
    IEnumerable<IShiftableDemandProfile> ShiftableDemands)
{
    /// <summary>
    /// Iterate differing charge energies to arrive at the optimal given the predicted generation and demand.
    /// </summary>
    public Evaluation DecideStrategy()
    {
        DateTime fromDate = DemandProfile.Starting;
        DateTime toDate = DemandProfile.Until;

        // Iterate through options for shiftable demand.
        // Fit the highest priority and largest demand in first, and then iteratively the smaller ones.
        var orderedShiftableDemands = ShiftableDemands
            .OrderBy(demand => demand.Priority)
            .ThenByDescending(demand => demand
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
                    .Where(f => f.Demand.Until < toDate) // Don't allow to overrun main calculation period
                    .Where(f => f.Demand.Starting.TimeOfDay >= shiftableDemand.Earliest.ToTimeSpan())
                    .Where(f => f.Demand.Until.TimeOfDay <= shiftableDemand.Latest.ToTimeSpan())
            ));

        Evaluation decision = IterateChargeRates(Enumerable.Empty<IDemandProfile>());

        List<(TimeSpan ShiftedBy, IDemandProfile DemandProfile, Evaluation Decision)> completed = new();
        foreach (var s in shiftableDemandsAsProfiles)
        {
            var optimal = s.Trials
                .Select(t => ((t.ShiftedBy, t.Demand, Decision: IterateChargeRates(completed.Select(f => f.DemandProfile).Concat(new[] { t.Demand })))))
                .ToArray()
                .OrderBy(f => f.Decision.TotalCost)
                .First();

            completed.Add((optimal.ShiftedBy, optimal.Demand, optimal.Decision));

            decision = completed.Last().Decision;
            Debug.WriteLine($"Charge rate: {decision.ChargeRateLimit?.ToString("F3")} Undercharge: {decision.UnderchargeEnergy.ToString("F1")} Overcharge: {decision.OverchargeEnergy.ToString("F1")} Cost: £{decision.TotalCost.ToString("F2")} {s.Name}: {completed.Last().ShiftedBy}");
        }

        return decision;
    }

    private IEnumerable<TimeSpan> CreateTrialTimespans(DateTime fromDate, DateTime toDate)
    {
        TimeSpan ts = TimeSpan.Zero;

        while (fromDate + ts < toDate)
        {
            yield return ts;
            ts += TimeSpan.FromMinutes(30);
        }
    }

    private Evaluation IterateChargeRates(IEnumerable<IDemandProfile> shiftableDemandsAsProfiles)
    {
        var chargeRates = Enumerable
            .Range(1, 100)
            .Select(percent => PlantTemplate.ChargeRateAtScalar((float)percent / 100.0f));

        var results = chargeRates.Select(chargeLimit => new Calculator(PlantTemplate).Calculate(
                DemandProfile,
                shiftableDemandsAsProfiles,
                GenerationProfile,
                ChargeProfile,
                PricingProfile,
                InitialState,
                chargeLimit
            ))
            .ToArray()
            .OrderBy(f => f.TotalCost);

        var resultWithOptimalChargeRate = results.First();

        return resultWithOptimalChargeRate;
    }
}
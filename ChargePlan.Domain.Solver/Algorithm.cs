using System.Diagnostics;
using System.Linq;
using MathNet.Numerics.Interpolation;

namespace ChargePlan.Domain.Solver;

public record Algorithm(
    IPlant PlantTemplate,
    IDemandProfile DemandProfile,
    IGenerationProfile GenerationProfile,
    IChargeProfile ChargeProfile,
    IPricingProfile PricingProfile,
    IExportProfile ExportProfile,
    PlantState InitialState,
    IEnumerable<IShiftableDemandProfile> ShiftableDemands,
    HashSet<string> CompletedDemands,
    DateTime? ExplicitStartDate)
{
    /// <summary>
    /// Iterate differing charge energies to arrive at the optimal given the predicted generation and demand.
    /// </summary>
    public Recommendations DecideStrategy()
    {
        // First decision is based just on the main demand profile.
        Evaluation evaluation = IterateChargeRates(Enumerable.Empty<IDemandProfile>());

        DateTime fromDate = (ExplicitStartDate ?? DemandProfile.Starting.OrAtEarliest(DateTime.Now)).ToClosestHour();
        DateTime toDate = DemandProfile.Until;

        // Iterate through options for shiftable demand.
        // For each day, fit the highest priority and largest demand in first, and then iteratively the smaller ones.
        // Skip any which have already been completed recently.
        var orderedShiftableDemands = ShiftableDemands
            .Where(demand => CompletedDemands.Contains(demand.AsDemandHash()) == false)
            .OrderBy(demand => demand.WithinDayRange?.From ?? DateTime.MaxValue)
            .ThenBy(demand => demand.Priority)
            .ThenByDescending(demand => demand
                .AsDemandProfile(fromDate)
                .AsSpline(StepInterpolation.Interpolate)
                .Integrate(fromDate.AsTotalHours(), toDate.AsTotalHours()))
            .ToArray();

        var shiftByTimespans = CreateTrialTimespans(fromDate, toDate);
        var shiftableDemandsAsTrialProfiles = orderedShiftableDemands
            .Select(shiftableDemand =>
            (
                ShiftableDemand: shiftableDemand,
                Trials: shiftByTimespans
                    .Select(ts => (StartAt: fromDate.Add(ts), Demand: shiftableDemand.AsDemandProfile(fromDate.Add(ts)))) // Apply the profile at each trial hour
                    .Where(f => f.Demand.Until < toDate) // Don't allow to overrun main calculation period
                    .Where(f => f.Demand.Starting.TimeOfDay >= shiftableDemand.Earliest.ToTimeSpan())
                    .Where(f => f.Demand.Starting.TimeOfDay <= shiftableDemand.Latest.ToTimeSpan())
                    .Where(f => shiftableDemand.WithinDayRange == null || (f.Demand.Starting >= shiftableDemand.WithinDayRange?.From && f.Demand.Until <= shiftableDemand.WithinDayRange?.To))
                    .ToArray()
            ))
            .Where(f => f.Trials.Any()) // Exclude demands that have missed this window totally i.e. likely have already happened
            .ToArray();

        var completedShiftableDemandOptimisations = new List<(IShiftableDemandProfile ShiftableDemand, DateTime StartAt, decimal AddedCost, IDemandProfile DemandProfile)>();
        foreach (var s in shiftableDemandsAsTrialProfiles)
        {
            // Take the previously-decided shiftable demands...
            var completedDemands = completedShiftableDemandOptimisations.Select(f => f.DemandProfile);

            // ...and append this shiftable demand to the end of that list, for each of its trials.
            // Ignore trials which are too soon.
            var optimal = s.Trials
                .Where(f => !completedShiftableDemandOptimisations.Any(g => s.ShiftableDemand.IsTooSoonToRepeat(g.ShiftableDemand, g.StartAt, f.StartAt)))
                .Select(t => ((
                    s.ShiftableDemand,
                    t.StartAt,
                    t.Demand,
                    Evaluation: IterateChargeRates(completedDemands.Concat(new[] { t.Demand })))))
                .ToArray()
                .OrderBy(f => f.Evaluation.TotalCost) // Order by the lowest total cost trial...
                .FirstOrDefault(); // ...and declare that as "optimal"

            if (optimal != default)
            {
                // We now have the optimal version of this shiftable demand. Add it to the completed results.
                completedShiftableDemandOptimisations.Add((optimal.ShiftableDemand, optimal.StartAt, optimal.Evaluation.TotalCost - evaluation.TotalCost, optimal.Demand));

                // Copy this latest evaluation as being the latest.
                evaluation = optimal.Evaluation;
            }
        }

        return new Recommendations(
            evaluation,
            completedShiftableDemandOptimisations
                .OrderBy(f => f.StartAt)
                .Select(f => new ShiftableDemandRecommendation(f.ShiftableDemand, f.StartAt, f.AddedCost, f.ShiftableDemand.AsDemandHash()))
        );
    }

    private IEnumerable<TimeSpan> CreateTrialTimespans(DateTime fromDate, DateTime toDate)
    {
        TimeSpan ts = TimeSpan.Zero;

        while (fromDate + ts < toDate)
        {
            yield return ts;
            ts += TimeSpan.FromMinutes(15);
        }
    }

    private Evaluation IterateChargeRates(IEnumerable<IDemandProfile> shiftableDemandsAsProfiles)
    {
        var chargeRates = Enumerable
            .Range(0, 101) // Go between 0 and 100%
            .Chunk(5) // ...in steps of n%
            .Select(percent => PlantTemplate.ChargeRateAtScalar((float)percent.First() / 100.0f));

        var results = chargeRates.Select(chargeLimit => new Calculator(PlantTemplate).Calculate(
                DemandProfile,
                shiftableDemandsAsProfiles,
                GenerationProfile,
                ChargeProfile,
                PricingProfile,
                ExportProfile,
                InitialState,
                chargeLimit,
                ExplicitStartDate
            ))
            .ToArray()
            .OrderBy(f => f.TotalCost);

        var resultWithOptimalChargeRate = results.First();

        return resultWithOptimalChargeRate;
    }
}
using System.Diagnostics;
using ChargePlan.Domain.Solver.GoalSeeking;

namespace ChargePlan.Domain.Solver;

public record Algorithm(
    IPlant PlantTemplate,
    IDemandProfile DemandProfile,
    IGenerationProfile GenerationProfile,
    IChargeProfile? FixedChargeProfile,
    IPricingProfile PricingProfile,
    IExportProfile ExportProfile,
    IInterpolationFactory InterpolationFactory,
    PlantState InitialState,
    IEnumerable<IShiftableDemandProfile> ShiftableDemands,
    HashSet<string> CompletedDemands,
    DateTimeOffset? ExplicitStartDate,
    AlgorithmPrecision AlgorithmPrecision)
{
    /// <summary>
    /// Iterate differing charge energies to arrive at the optimal given the predicted generation and demand.
    /// </summary>
    /// <param name="autoForceExport">If true, will automatically look for opportunities to force-export from the battery.</param>
    public Recommendations DecideStrategy(bool autoForceExport = true)
    {
        DateTimeOffset fromDate = (ExplicitStartDate ?? new DateTimeOffset(DemandProfile.Starting).OrAtEarliest(DateTimeOffset.Now)).ToClosestHour();
        DateTimeOffset toDate = DemandProfile.Until;

        if (autoForceExport)
        {
            // double idealExportEnergy = new BinaryDivisionSeeker().Iterations(
            //     goal: 0.0,
            //     startValue: PricingProfile.AsSplineOrZero(InterpolationFactory.InterpolatePricing).Average(fromDate, toDate, TimeSpan.FromHours(1)),
            //     createModel: value => 
            // );
        }

        // Establish baseline based just on the main demand profile.
        Calculator calculator = CreateCalculator(
            GetOrCreateChargeProfile(() => (fromDate, toDate, Array.Empty<IDemandProfile>())),
            Array.Empty<IDemandProfile>());

        Evaluation evaluation = calculator.Calculate(
            InitialState,
            AlgorithmPrecision.TimeStep,
            null,
            null,
            ExplicitStartDate
        );

        // Iterate through options for shiftable demand.
        // For each day, fit the highest priority and largest demand in first, and then iteratively the smaller ones.
        // Skip any which have already been completed recently.

        // Take each shiftable demand and offset it by periods of time, to create "trials" for each demand.
        var pendingTrials = CreateShiftableDemandTrials(fromDate, toDate);

        var optimalTrials = new List<(IShiftableDemandProfile ShiftableDemand, DateTimeOffset StartAt, decimal AddedCost, IDemandProfile DemandProfile)>();
        foreach (var demand in pendingTrials)
        {
            // Take the previously-decided shiftable demands...
            var completedDemands = optimalTrials.Select(f => f.DemandProfile);

            // ...and append this shiftable demand to the end of that list, for each of its trials.
            // Ignore trials which are too soon.
            var validTrials = demand.Trials
                .Where(f => !optimalTrials.Any(g => demand.ShiftableDemand.IsTooSoonToRepeat(g.ShiftableDemand, g.StartAt.LocalDateTime, f.StartAt.LocalDateTime)))
                .Select(t => (
                    OriginalProfile: demand.ShiftableDemand,
                    StartAt: t.StartAt,
                    ThisDemand: t.Demand,
                    ThisAndOtherDemands: completedDemands.Append(t.Demand)
                ))
                .ToArray();

            // For each trial of this shiftable demand, evaluate the energy and also the ideal charge rates.
            var trialResults = validTrials
                .Select(f =>
                {
                    var thisCalculator = calculator with
                    {
                        ChargeProfile = GetOrCreateChargeProfile(() => (fromDate, toDate, f.ThisAndOtherDemands)), // This is a best-guess but ignores the battery limit. It's a good upper starting point.
                        SpecificDemandProfiles = f.ThisAndOtherDemands
                    };

                    var chargeRateAttempts = new BinaryDivisionSeeker().Iterations(
                        goal: 0.0, // Zero cost, ideally
                        startValue: 1.0, // Start at full charge rate
                        createModel: chargeRate => PlantTemplate.ChargeRateAtScalar((float)chargeRate),
                        executeModel: model => (double)thisCalculator.Calculate(InitialState, AlgorithmPrecision.TimeStep, model, null, ExplicitStartDate).TotalCost
                    ).Take(8);

                    float chargeRate = chargeRateAttempts
                        .OrderByDescending(f => f.DeltaToGoal)
                        .ThenByDescending(f => f.Model)
                        .Last()
                        .Model;

                    var dischargeRateAttempts = new BinaryDivisionSeeker().Iterations(
                        goal: 0.0, // Zero cost, ideally
                        startValue: 1.0, // Start at full charge rate
                        createModel: dischargeRate => PlantTemplate.DischargeRateAtScalar((float)dischargeRate),
                        executeModel: model => (double)thisCalculator.Calculate(InitialState, AlgorithmPrecision.TimeStep, chargeRate, model, ExplicitStartDate).TotalCost
                    ).Take(8);

                    float dischargeRate = dischargeRateAttempts
                        .OrderByDescending(f => f.DeltaToGoal)
                        .ThenByDescending(f => f.Model)
                        .Last()
                        .Model;

                    var optimal = thisCalculator.Calculate(InitialState, AlgorithmPrecision.TimeStep, chargeRate, dischargeRate, ExplicitStartDate);

                    return (Trial: f, Evaluation: optimal);
                })
                .ToArray();
            
            // Order by the lowest effective cost and where it's being started as soon as possible.
            var byLowestCost = trialResults
                .Select(f => (
                    f.Trial.OriginalProfile,
                    f.Trial.StartAt,
                    f.Trial.ThisDemand,
                    f.Evaluation,
                    ActualAddedCost: f.Evaluation.TotalCost - evaluation.TotalCost,
                    EffectiveAddedCost: f.Trial.OriginalProfile.EffectiveCost(fromDate, f.Trial.StartAt, f.Evaluation.TotalCost - evaluation.TotalCost)
                ))
                .OrderBy(f => f.EffectiveAddedCost) // Order by the lowest total cost trial (applying threshold)...
                .ThenBy(f => f.StartAt)
                .ToArray();
            
            var optimal = byLowestCost.FirstOrDefault(); // ...and declare that as "optimal"

            if (optimal != default)
            {
                // We now have the optimal version of this shiftable demand. Add it to the completed results.
                optimalTrials.Add((optimal.OriginalProfile, optimal.StartAt, optimal.Evaluation.TotalCost - evaluation.TotalCost, optimal.ThisDemand));

                // Copy this latest evaluation as being the latest.
                evaluation = optimal.Evaluation;
            }
        }

        return new Recommendations(
            evaluation,
            optimalTrials
                .OrderBy(f => f.StartAt)
                .Select(f => new ShiftableDemandRecommendation(f.ShiftableDemand.Name, f.ShiftableDemand.Type, f.StartAt, f.AddedCost, f.ShiftableDemand.AsDemandHash())),
            Array.Empty<DemandCompleted>()
        );
    }

    private Calculator CreateCalculator(IChargeProfile chargeProfile, IEnumerable<IDemandProfile> shiftableDemands) => new Calculator(
        PlantTemplate,
        DemandProfile,
        shiftableDemands,
        GenerationProfile,
        chargeProfile,
        PricingProfile,
        ExportProfile,
        InterpolationFactory
    );

    private IChargeProfile GetOrCreateChargeProfile(Func<(DateTimeOffset fromDate, DateTimeOffset toDate, IEnumerable<IDemandProfile> knownShiftableDemands)> paramsIfCreationRequired)
    {
        if (FixedChargeProfile != null) return FixedChargeProfile;

        var (fromDate, toDate, knownShiftableDemands) = paramsIfCreationRequired();
        return CreateOptimalChargeProfile(fromDate, toDate, knownShiftableDemands);
    }

    /// <summary>
    /// Take all the shiftable demands and create trial runs of them at different times of the day, according to their rules.
    /// </summary>
    private IEnumerable<ShiftableDemandProfileTrials> CreateShiftableDemandTrials(DateTimeOffset fromDate, DateTimeOffset toDate)
    {
        var orderedShiftableDemands = ShiftableDemands
            .Where(demand => CompletedDemands.Contains(demand.AsDemandHash()) == false)
            .OrderBy(demand => demand.WithinDayRange?.From ?? DateTime.MaxValue)
            .ThenBy(demand => demand.Priority)
            .ThenByDescending(demand => demand
                .AsDemandProfile(fromDate.LocalDateTime)
                .AsSpline(InterpolationFactory.InterpolateShiftableDemand)
                .Integrate(fromDate.AsTotalHours(), toDate.AsTotalHours()))
            .ToArray();

        var shiftByTimespans = CreateTrialTimespans(fromDate, toDate);
        var shiftableDemandsAsTrialProfiles = orderedShiftableDemands
            .Select(shiftableDemand =>
            (
                ShiftableDemand: shiftableDemand,
                Trials: shiftByTimespans
                    .Select(ts => (StartAt: fromDate.Add(ts), Demand: shiftableDemand.AsDemandProfile(fromDate.LocalDateTime.Add(ts)))) // Apply the profile at each trial hour
                    .Where(f => f.Demand.Until < toDate) // Don't allow to overrun main calculation period
                    .Where(f => f.Demand.Starting.TimeOfDay >= shiftableDemand.Earliest.ToTimeSpan())
                    .Where(f => f.Demand.Starting.TimeOfDay <= shiftableDemand.Latest.ToTimeSpan())
                    .Where(f => shiftableDemand.WithinDayRange == null || (f.Demand.Starting >= shiftableDemand.WithinDayRange?.From && f.Demand.Until <= shiftableDemand.WithinDayRange?.To))
                    .ToArray()
            ))
            .Where(f => f.Trials.Any()) // Exclude demands that have missed this window totally i.e. likely have already happened
            .Select(f => new ShiftableDemandProfileTrials(f.ShiftableDemand, f.Trials))
            .ToArray();

        return shiftableDemandsAsTrialProfiles;
    }

    private record ShiftableDemandProfileTrials(
        IShiftableDemandProfile ShiftableDemand,
        (DateTimeOffset StartAt, IDemandProfile Demand)[] Trials
    );

    private IEnumerable<TimeSpan> CreateTrialTimespans(DateTimeOffset fromDate, DateTimeOffset toDate)
    {
        TimeSpan ts = TimeSpan.Zero;

        while (fromDate + ts < toDate)
        {
            yield return ts;
            ts += AlgorithmPrecision.ShiftBy;
        }
    }

    private IChargeProfile CreateOptimalChargeProfile(DateTimeOffset fromDate, DateTimeOffset toDate, IEnumerable<IDemandProfile> knownShiftableDemands)
    {
        TimeSpan stepAvg = TimeSpan.FromMinutes(1);
        TimeSpan stepOutput = TimeSpan.FromMinutes(10);

        var pricing = PricingProfile.AsSplineOrZero(InterpolationFactory.InterpolatePricing);

        // Infer it from the pricing, per day.
        DateTimeOffset day = fromDate.Date;
        List<ChargeValue> chargeValues = new();
        while (day < toDate)
        {
            DateTimeOffset start = day;
            DateTimeOffset end = day.AddDays(1);

            // Disregarding any charge profiles that have been decided up until now; how much energy is required for this time period.
            float kWhRequired = CreateCalculator(SynthesisedPowerProfile.Empty(), knownShiftableDemands).DemandEnergyBetween(start, end);

            // Calculate charge times based on the pricing profile to achieve the desired kWh.
            // Initially, assume maximum charge rate.
            // Then trim any excess charging by modifying the charge power rate.
            var optimal = CreateOptimalChargeProfilesFromPricing(start, end, pricing, kWhRequired, stepAvg, stepOutput)
                .Take(AlgorithmPrecision.AutoChargeWindow.MaxPricingIterations)
                .ToArray()
                .OrderByDescending(f => f.kWhExcess > 0.0f) // Prefer being slightly over...
                .ThenBy(f => Math.Abs(f.kWhExcess)) // But other than that, just look for whatever's closest.
                .First()
                .Profile;

            optimal = ModifyOptimalChargeProfilesUsingChargeRate(start, end, optimal, kWhRequired)
                .Take(AlgorithmPrecision.AutoChargeWindow.MaxRateIterations)
                .ToArray()
                .OrderByDescending(f => f.kWhExcess > 0.0f)
                .ThenBy(f => Math.Abs(f.kWhExcess))
                .First()
                .Profile;

            chargeValues.AddRange(optimal.Values);

            day = day.AddDays(1);
        }

        return new SynthesisedPowerProfile(chargeValues);
    }

    private IEnumerable<(double kWhExcess, IChargeProfile Profile)> CreateOptimalChargeProfilesFromPricing(DateTimeOffset start, DateTimeOffset end, IInterpolation pricing, float kWhRequired, TimeSpan stepAnalyse, TimeSpan stepOutput)
        => new BinaryDivisionSeeker(ModelValueLimiter: price => Math.Max(0.0, price)).Iterations(
            goal: kWhRequired,
            startValue: pricing.Average(start, end, stepAnalyse),
            createModel: price => pricing.ToChargeProfile(price, start, end, PlantTemplate.ChargeRateAtScalar(1.0f)),
            executeModel: model => model.AsSplineOrZero(InterpolationFactory.InterpolateCharging).Integrate(start, end)
        );

    private IEnumerable<(double kWhExcess, IChargeProfile Profile)> ModifyOptimalChargeProfilesUsingChargeRate(DateTimeOffset start, DateTimeOffset end, IChargeProfile chargeProfile, float kWhRequired)
        => new BinaryDivisionSeeker(ModelValueLimiter: chargeRate => Math.Max(0.0, Math.Min(1.0, chargeRate))).Iterations(
            goal: kWhRequired,
            startValue: 1.0,
            createInitialModel: chargeRate => chargeProfile,
            executeModel: model => model.AsSplineOrZero(InterpolationFactory.InterpolateCharging).Integrate(start, end),
            reviseModel: (chargeRate, model) => new SynthesisedPowerProfile(model.Values
                .Select(f => f with { Power = f.Power > 0.0f ? PlantTemplate.ChargeRateAtScalar((float)chargeRate) : 0.0f } )
                .ToList())
        );
}
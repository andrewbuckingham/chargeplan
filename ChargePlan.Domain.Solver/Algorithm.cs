using System.Diagnostics;

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
    public Recommendations DecideStrategy()
    {
        DateTimeOffset fromDate = (ExplicitStartDate ?? new DateTimeOffset(DemandProfile.Starting).OrAtEarliest(DateTimeOffset.Now)).ToClosestHour();
        DateTimeOffset toDate = DemandProfile.Until;

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
        foreach (var s in pendingTrials)
        {
            // Take the previously-decided shiftable demands...
            var completedDemands = optimalTrials.Select(f => f.DemandProfile);

            // ...and append this shiftable demand to the end of that list, for each of its trials.
            // Ignore trials which are too soon.
            var validTrials = s.Trials
                .Where(f => !optimalTrials.Any(g => s.ShiftableDemand.IsTooSoonToRepeat(g.ShiftableDemand, g.StartAt.LocalDateTime, f.StartAt.LocalDateTime)))
                .Select(t => (
                    OriginalProfile: s.ShiftableDemand,
                    StartAt: t.StartAt,
                    ThisDemand: t.Demand,
                    ThisAndOtherDemands: completedDemands.Append(t.Demand)
                ))
                .ToArray();

            var trialResults = validTrials
                .Select(f =>
                {
                    var thisCalculator = calculator with
                    {
                        ChargeProfile = GetOrCreateChargeProfile(() => (fromDate, toDate, f.ThisAndOtherDemands)), // This is a best-guess but ignores the battery limit. It's a good upper starting point.
                        SpecificDemandProfiles = f.ThisAndOtherDemands
                    };

                    var optimal = CreateChargeRateOptions()
                        .Select(chargeLimit => calculator.Calculate(InitialState, AlgorithmPrecision.TimeStep, chargeLimit, null, ExplicitStartDate))
                        .ToArray()
                        .OrderBy(f => f.TotalCost)
                        .First();

                    optimal = CreateChargeRateOptions()
                        .Select(dischargeLimit => thisCalculator.Calculate(InitialState, AlgorithmPrecision.TimeStep, optimal.ChargeRateLimit, dischargeLimit, ExplicitStartDate))
                        .ToArray()
                        .OrderBy(f => f.TotalCost)
                        .First();

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

    private IEnumerable<float> CreateChargeRateOptions() => AlgorithmPrecision.IterateInPercents == null	
        ? new[] { PlantTemplate.ChargeRateAtScalar(1.0f) }	
        : Enumerable	
            .Range(0, 101) // Go between 0 and 100%	
            .Chunk(AlgorithmPrecision.IterateInPercents ?? 100) // ...in steps of n%	
            .Select(f => f.First())	
            .Append(100)	
            .Distinct()	
            .Select(percent => PlantTemplate.ChargeRateAtScalar((float)percent / 100.0f))	
            .ToArray();	

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

    public IChargeProfile CreateOptimalChargeProfile(DateTimeOffset fromDate, DateTimeOffset toDate, IEnumerable<IDemandProfile> knownShiftableDemands)
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
            float kWhRequired = CreateCalculator(SynthesisedChargeProfile.Empty(), knownShiftableDemands).DemandEnergyBetween(start, end);

            // Calculate charge times based on the pricing profile to achieve the desired kWh.
            // Initially, assume maximum charge rate.
            // Then trim any excess charging by modifying the charge power rate.
            var optimal = CreateOptimalChargeProfilesFromPricing(start, end, pricing, kWhRequired, stepAvg, stepOutput, AlgorithmPrecision.AutoChargeWindow.MaxPricingIterations)
                .ToArray()
                .OrderByDescending(f => f.kWhExcess > 0.0f) // Prefer being slightly over...
                .ThenBy(f => Math.Abs(f.kWhExcess)) // But other than that, just look for whatever's closest.
                .First()
                .Profile;

            optimal = ModifyOptimalChargeProfilesUsingChargeRate(start, end, optimal, kWhRequired, AlgorithmPrecision.AutoChargeWindow.MaxRateIterations)
                .ToArray()
                .OrderByDescending(f => f.kWhExcess > 0.0f)
                .ThenBy(f => Math.Abs(f.kWhExcess))
                .First()
                .Profile;

            chargeValues.AddRange(optimal.Values);

            day = day.AddDays(1);
        }

        return new SynthesisedChargeProfile(chargeValues);
    }

    private IEnumerable<(double kWhExcess, IChargeProfile Profile)> CreateOptimalChargeProfilesFromPricing(DateTimeOffset start, DateTimeOffset end, IInterpolation pricing, float kWhRequired, TimeSpan stepAnalyse, TimeSpan stepOutput, int maxIterations)
    {
        // Good starting point is the average price.
        double thresholdPrice = pricing.Average(start, end, stepAnalyse);

        // This method always uses peak charge rate.
        float chargePower = PlantTemplate.ChargeRateAtScalar(1.0f);

        // Next time around the loop, we'll adjust the threshold up or down by half of the current value.
        double nextPriceAdjustmentAbs = thresholdPrice / 2;
        double previousAdjustmentDirection = -1.0;

        // Iterate
        for (int option = 0; option < maxIterations; option++)
        {
            // Create a charge profile which charges when the price is less than the threshold.
            List<ChargeValue> chargeValues = new();
            DateTimeOffset instant = start;
            while (instant < end)
            {
                bool shouldPower = pricing.Interpolate(instant) < thresholdPrice;
                chargeValues.Add(new ChargeValue(instant.DateTime, shouldPower ? chargePower : 0.0f));
                instant += stepOutput;
            }

            chargeValues = chargeValues.Take(1).Concat(chargeValues
                .Zip(chargeValues.Skip(1))
                .Where(f => f.First.Power != f.Second.Power)
                .Select(f => f.Second))
                .ToList();

            // Assess how much energy would be charged into the battery from that trial charging profile.
            IChargeProfile trial = new SynthesisedChargeProfile(chargeValues);
            double kWhYielded = trial.AsSplineOrZero(InterpolationFactory.InterpolateCharging).Integrate(start, end);
            var result = (kWhYielded - kWhRequired, trial);
            yield return result;

            // Next time around the loop, modify the trial price up/down depending if there was too much/little energy acquired.
            double nextAdjustmentDirection = kWhYielded - kWhRequired > 0.0 ? -1.0 : 1.0;

            // Use a finer pricing threshold next time, if we've crossed the threshold.
            if (previousAdjustmentDirection != nextAdjustmentDirection)
            {
                nextPriceAdjustmentAbs /= 2;
            }

            thresholdPrice += nextPriceAdjustmentAbs * nextAdjustmentDirection;
            thresholdPrice = Math.Max(0.0f, thresholdPrice);

            previousAdjustmentDirection = nextAdjustmentDirection;
        }
    }

    private IEnumerable<(double kWhExcess, IChargeProfile Profile)> ModifyOptimalChargeProfilesUsingChargeRate(DateTimeOffset start, DateTimeOffset end, IChargeProfile chargeProfile, float kWhRequired, int maxIterations)
    {
        // Good starting point is full-power.
        float chargeRateScalar = 1.0f;

        // Next time around the loop, we'll adjust the threshold up or down by half of the current value.
        float nextScalarAdjustmentAbs = chargeRateScalar / 2.0f;
        double previousAdjustmentDirection = -1.0;

        // Iterate
        for (int option = 0; option < maxIterations; option++)
        {
            // Because we were already supplied with a starting point, yield this one first.
            double kWhYielded = chargeProfile.AsSplineOrZero(InterpolationFactory.InterpolateCharging).Integrate(start, end);
            var result = (kWhYielded - kWhRequired, chargeProfile);
            yield return result;

            // Next time around the loop, modify the charge power up/down depending if there was too little/much energy acquired.
            float nextAdjustmentDirection = kWhYielded - kWhRequired > 0.0 ? -1.0f : 1.0f;

            // Use a finer pricing threshold next time, if we've crossed the threshold.
            if (previousAdjustmentDirection != nextAdjustmentDirection)
            {
                nextScalarAdjustmentAbs /= 2;
            }

            chargeRateScalar += nextScalarAdjustmentAbs * nextAdjustmentDirection;
            chargeRateScalar = Math.Max(0.0f, Math.Min(1.0f, chargeRateScalar));

            // Create the next one.
            chargeProfile = new SynthesisedChargeProfile(chargeProfile.Values
                .Select(f => f with { Power = f.Power > 0.0f ? PlantTemplate.ChargeRateAtScalar(chargeRateScalar) : 0.0f } )
                .ToList());

            previousAdjustmentDirection = nextAdjustmentDirection;
        }
    }
}
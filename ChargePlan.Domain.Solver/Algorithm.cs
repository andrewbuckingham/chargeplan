namespace ChargePlan.Domain.Solver;

public record Algorithm(
    IPlant PlantTemplate,
    IDemandProfile DemandProfile,
    IGenerationProfile GenerationProfile,
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

        IChargeProfile chargeProfile /*TODO*/ = CreateOptimalChargeProfiles(fromDate, toDate).Last();

        // First decision is based just on the main demand profile.
        Evaluation evaluation = IterateChargeRates(Enumerable.Empty<IDemandProfile>(), chargeProfile);

        // Iterate through options for shiftable demand.
        // For each day, fit the highest priority and largest demand in first, and then iteratively the smaller ones.
        // Skip any which have already been completed recently.
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
            .ToArray();

        var completedShiftableDemandOptimisations = new List<(IShiftableDemandProfile ShiftableDemand, DateTimeOffset StartAt, decimal AddedCost, IDemandProfile DemandProfile)>();
        foreach (var s in shiftableDemandsAsTrialProfiles)
        {
            // Take the previously-decided shiftable demands...
            var completedDemands = completedShiftableDemandOptimisations.Select(f => f.DemandProfile);

            // ...and append this shiftable demand to the end of that list, for each of its trials.
            // Ignore trials which are too soon.
            var trialResults = s.Trials
                .Where(f => !completedShiftableDemandOptimisations.Any(g => s.ShiftableDemand.IsTooSoonToRepeat(g.ShiftableDemand, g.StartAt.LocalDateTime, f.StartAt.LocalDateTime)))
                .Select(t => ((
                    s.ShiftableDemand,
                    t.StartAt,
                    t.Demand,
                    Evaluation: IterateChargeRates(completedDemands.Append(t.Demand), chargeProfile))))
                .ToArray();
            
            // Order by the lowest effective cost and where it's being started as soon as possible.
            var byLowestCost = trialResults
                .Select(f => ((
                    f.ShiftableDemand,
                    f.StartAt,
                    f.Demand,
                    f.Evaluation,
                    ActualAddedCost: f.Evaluation.TotalCost - evaluation.TotalCost,
                    EffectiveAddedCost: f.ShiftableDemand.EffectiveCost(fromDate, f.StartAt, f.Evaluation.TotalCost - evaluation.TotalCost)
                )))
                .OrderBy(f => f.EffectiveAddedCost) // Order by the lowest total cost trial (applying threshold)...
                .ThenBy(f => f.StartAt)
                .ToArray();
            
            var optimal = byLowestCost.FirstOrDefault(); // ...and declare that as "optimal"

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
                .Select(f => new ShiftableDemandRecommendation(f.ShiftableDemand.Name, f.ShiftableDemand.Type, f.StartAt, f.AddedCost, f.ShiftableDemand.AsDemandHash())),
            Array.Empty<DemandCompleted>()
        );
    }

    private IEnumerable<TimeSpan> CreateTrialTimespans(DateTimeOffset fromDate, DateTimeOffset toDate)
    {
        TimeSpan ts = TimeSpan.Zero;

        while (fromDate + ts < toDate)
        {
            yield return ts;
            ts += AlgorithmPrecision.ShiftBy;
        }
    }

    private Evaluation IterateChargeRates(IEnumerable<IDemandProfile> shiftableDemandsAsProfiles, IChargeProfile chargeProfile)
    {
        int[] percentages;
        
        if (AlgorithmPrecision.IterateInPercents == null)
        {
            percentages = new[] { 100 };
        }
        else
        {
            percentages = Enumerable
                .Range(0, 101) // Go between 0 and 100%
                .Chunk(AlgorithmPrecision.IterateInPercents ?? 100) // ...in steps of n%
                .Select(f => f.First())
                .Append(100)
                .Distinct()
                .ToArray();
        }

        // Optimise for the charge amount first.

        var chargeRates = percentages.Select(percent => PlantTemplate.ChargeRateAtScalar((float)percent / 100.0f));

        var results = chargeRates.Select(chargeLimit => new Calculator(PlantTemplate).Calculate(
                DemandProfile,
                shiftableDemandsAsProfiles,
                GenerationProfile,
                chargeProfile,
                PricingProfile,
                ExportProfile,
                InterpolationFactory,
                InitialState,
                AlgorithmPrecision.TimeStep,
                chargeLimit,
                null,
                ExplicitStartDate
            ))
            .ToArray()
            .OrderBy(f => f.TotalCost);

        var resultWithOptimalChargeRate = results.First();


        // Now optimise for the discharge rate.

        var dischargeRates = percentages.Select(percent => PlantTemplate.DischargeRateAtScalar((float)percent / 100.0f));

        results = dischargeRates.Select(dischargeLimit => new Calculator(PlantTemplate).Calculate(
                DemandProfile,
                shiftableDemandsAsProfiles,
                GenerationProfile,
                chargeProfile,
                PricingProfile,
                ExportProfile,
                InterpolationFactory,
                InitialState,
                AlgorithmPrecision.TimeStep,
                resultWithOptimalChargeRate.ChargeRateLimit,
                dischargeLimit,
                ExplicitStartDate
            ))
            .ToArray()
            .OrderBy(f => f.TotalCost)
            .ThenBy(f => f.DischargeRateLimit)
            ;

        resultWithOptimalChargeRate = results.First();

        return resultWithOptimalChargeRate;
    }

    public IEnumerable<IChargeProfile> CreateOptimalChargeProfiles(DateTimeOffset fromDate, DateTimeOffset toDate)
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

            var forDay = CreateOptimalChargeProfiles(start, end, pricing, 2.8f /*TODO*/, 4.0f /*TODO*/, stepAvg, stepOutput);
            chargeValues.AddRange(forDay.Last().Profile.Values);

            day = day.AddDays(1);
        }

        yield return new SynthesisedChargeProfile(chargeValues);
    }

    private IEnumerable<(double kWhExcess, IChargeProfile Profile)> CreateOptimalChargeProfiles(DateTimeOffset start, DateTimeOffset end, IInterpolation pricing, float chargePower, float kWhRequired, TimeSpan stepAnalyse, TimeSpan stepOutput, int maxOptions = 8)
    {
        // Good starting point is the average price.
        double thresholdPrice = pricing.Average(start, end, stepAnalyse);

        // Next time around the loop, we'll adjust the threshold up or down by half of the current value.
        double nextPriceAdjustmentAbs = thresholdPrice / 2;

        // Iterate
        for (int option = 0; option < maxOptions; option++)
        {
            // Create a charge profile which charges when the price is less than the threshold.
            List<ChargeValue> chargeValues = new();
            while (start < end)
            {
                bool shouldPower = pricing.Interpolate(start) < thresholdPrice;
                chargeValues.Add(new ChargeValue(start.DateTime, shouldPower ? chargePower : 0.0f));
                start += stepOutput;
            }

            // Assess how much energy would be charged into the battery from that trial charging profile.
            IChargeProfile trial = new SynthesisedChargeProfile(chargeValues);
            double kWhYielded = trial.AsSplineOrZero(InterpolationFactory.InterpolateCharging).Integrate(start, end);
            var result = (kWhYielded - kWhRequired, trial);

            // Next time around the loop, modify the trial price up/down depending if there was too little/much energy acquired.
            double nextTrialPriceAdjustmentDirection = kWhYielded - kWhRequired > 0.0 ? 1.0 : -1.0;
            thresholdPrice += nextPriceAdjustmentAbs * nextTrialPriceAdjustmentDirection;

            // Use a finer pricing threshold next time.
            nextPriceAdjustmentAbs /= 2;

            yield return result;
        }
    }
}
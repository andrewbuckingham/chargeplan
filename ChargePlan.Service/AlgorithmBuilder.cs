using ChargePlan.Service;

public record AlgorithmBuilder(IPlant PlantTemplate,
        DemandProfile DemandProfile,
        GenerationProfile GenerationProfile,
        ChargeProfile ChargeProfile,
        PricingProfile PricingProfile,
        PlantState InitialState,
        ShiftableDemand[] ShiftableDemands)
{
    public AlgorithmBuilder(IPlant plantTemplate, PlantState initialState)
        : this(plantTemplate, new(), new(), new(), new(), initialState, new ShiftableDemand[] {}) {}

    public AlgorithmBuilder WithHourlyGeneration(DateTime datum, params float[] hourlyFigures)
        => this with { GenerationProfile = new() { Values = hourlyFigures.Select((f, i) => new GenerationValue(datum.AddHours(i), f)).ToList() } };

    public AlgorithmBuilder WithDemand(IEnumerable<DemandValue> values) => this with { DemandProfile = new() { Values = values.ToList() } };
    public AlgorithmBuilder WithCharge(IEnumerable<ChargeValue> values) => this with { ChargeProfile = new() { Values = values.ToList() } };
    public AlgorithmBuilder WithPricing(IEnumerable<PricingValue> values) => this with { PricingProfile = new() { Values = values.ToList() } };
    public AlgorithmBuilder AddShiftableDemand(string name, IEnumerable<ShiftableDemandValue> demand, TimeOnly? noEarlierThan = null, TimeOnly? noLaterThan = null, ShiftableDemandPriority priority = ShiftableDemandPriority.Essential) => this with { ShiftableDemands = ShiftableDemands.Concat(new[] { new ShiftableDemand()
    {
        Name = name,
        Earliest = noEarlierThan ?? TimeOnly.MinValue,
        Latest = noLaterThan ?? TimeOnly.MaxValue,
        Priority = priority,
        Values = demand.ToList()
    } }).ToArray() };

    public Algorithm Build() => new Algorithm(PlantTemplate, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, InitialState, ShiftableDemands);
}
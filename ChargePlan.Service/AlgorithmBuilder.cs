using ChargePlan.Service;

public record AlgorithmBuilder(StorageProfile StorageProfile,
        DemandProfile DemandProfile,
        GenerationProfile GenerationProfile,
        ChargeProfile ChargeProfile,
        PricingProfile PricingProfile,
        CurrentState CurrentState,
        ShiftableDemand[] ShiftableDemands)
{
    public AlgorithmBuilder(StorageProfile storageProfile, CurrentState currentState)
        : this(storageProfile, new(), new(), new(), new(), currentState, new ShiftableDemand[] {}) {}

    public AlgorithmBuilder WithHourlyGeneration(DateTime datum, params float[] hourlyFigures)
        => this with { GenerationProfile = new() { Values = hourlyFigures.Select((f, i) => new GenerationValue(datum.AddHours(i), f)).ToList() } };

    public AlgorithmBuilder WithDemand(IEnumerable<DemandValue> values) => this with { DemandProfile = new() { Values = values.ToList() } };
    public AlgorithmBuilder WithCharge(IEnumerable<ChargeValue> values) => this with { ChargeProfile = new() { Values = values.ToList() } };
    public AlgorithmBuilder WithPricing(IEnumerable<PricingValue> values) => this with { PricingProfile = new() { Values = values.ToList() } };
    public AlgorithmBuilder AddShiftableDemand(string name, IEnumerable<ShiftableDemandValue> demand, TimeOnly? noEarlierThan = null, TimeOnly? noLaterThan = null) => this with { ShiftableDemands = ShiftableDemands.Concat(new[] { new ShiftableDemand()
    {
        Name = name,
        Earliest = noEarlierThan ?? TimeOnly.MinValue,
        Latest = noLaterThan ?? TimeOnly.MaxValue,
        Values = demand.ToList()
    } }).ToArray() };

    public Algorithm Build() => new Algorithm(StorageProfile, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, CurrentState, ShiftableDemands);
}
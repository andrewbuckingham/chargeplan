using ChargePlan.Service;

public record AlgorithmBuilder(StorageProfile StorageProfile,
        DemandProfile DemandProfile,
        GenerationProfile GenerationProfile,
        ChargeProfile ChargeProfile,
        PricingProfile PricingProfile,
        CurrentState CurrentState)
{
    public AlgorithmBuilder(StorageProfile storageProfile, CurrentState currentState)
        : this(storageProfile, new(), new(), new(), new(), currentState) {}

    public AlgorithmBuilder WithHourlyGeneration(DateTime datum, params float[] hourlyFigures)
        => this with { GenerationProfile = new() { Values = hourlyFigures.Select((f, i) => new GenerationValue(datum.AddHours(i), f)).ToList() } };

    public AlgorithmBuilder WithDemand(IEnumerable<DemandValue> values) => this with { DemandProfile = new() { Values = values.ToList() } };
    public AlgorithmBuilder WithCharge(IEnumerable<ChargeValue> values) => this with { ChargeProfile = new() { Values = values.ToList() } };
    public AlgorithmBuilder WithPricing(IEnumerable<PricingValue> values) => this with { PricingProfile = new() { Values = values.ToList() } };

    public Algorithm Build() => new Algorithm(StorageProfile, DemandProfile, GenerationProfile, ChargeProfile, PricingProfile, CurrentState);
}
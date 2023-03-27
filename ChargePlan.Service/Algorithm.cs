using System.Diagnostics;
using System.Linq;

namespace ChargePlan.Service;

public record Algorithm(
    StorageProfile StorageProfile,
    DemandProfile DemandProfile,
    GenerationProfile GenerationProfile,
    ChargeProfile ChargeProfile,
    PricingProfile PricingProfile,
    CurrentState CurrentState)
{
    /// <summary>
    /// Iterate differing charge energies to arrive at the optimal given the predicted generation and demand.
    /// </summary>
    public Decision DecideStrategy()
    {
        var chargeRates = Enumerable
            .Range(1, 100)
            .Select(percent => (float)percent * StorageProfile.MaxChargeKilowatts / 100.0f);

        var results = chargeRates.Select(chargeLimit => new Calculator().Calculate(
                StorageProfile,
                DemandProfile,
                GenerationProfile,
                ChargeProfile,
                PricingProfile,
                CurrentState,
                chargeLimit
            ))
            .OrderBy(f => f.TotalCost);

        return results.First();
    }
}
using ChargePlan.Domain;

namespace ChargePlan.Builder.Templates;

/// <summary>
/// A monetary value at an absolute time of the day.
/// Useful for the Unit Price and the Export Price.
/// </summary>
public record PriceAtAbsoluteTimes(List<(TimeOnly TimeOfDay, decimal PricePerUnit)> Values, string? Name = null)
{
    public PricingProfile AsPricingProfile(DateTime startAt) => new()
    {
        Values = Values
            .Select(f => new PricingValue(startAt.Date.ToLocalTime() + f.TimeOfDay.ToTimeSpan(), f.PricePerUnit))
            .ToList()
    };

    public ExportProfile AsExportProfile(DateTime startAt) => new()
    {
        Values = Values
            .Select(f => new ExportValue(startAt.Date.ToLocalTime() + f.TimeOfDay.ToTimeSpan(), f.PricePerUnit))
            .ToList()
    };
}
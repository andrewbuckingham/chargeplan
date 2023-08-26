using ChargePlan.Domain.Exceptions;
using ChargePlan.Domain;

namespace ChargePlan.Service.Entities.ForecastTuning;

/// <summary>
/// The actual energy produced by PV at a point in time, for comparison against forecast history.
/// </summary>
public class EnergyHistory
{
    public List<EnergyDatapoint> Values = new();
}

public record EnergyDatapoint(
    DateTimeOffset InHour,
    float Energy
);

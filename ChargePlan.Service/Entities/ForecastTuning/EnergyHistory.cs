using ChargePlan.Domain.Exceptions;
using ChargePlan.Domain;
using System.Diagnostics;

namespace ChargePlan.Service.Entities.ForecastTuning;

/// <summary>
/// The actual energy produced by PV at a point in time, for comparison against forecast history.
/// </summary>
[DebuggerDisplay("{Values[0]} .. {Values[Values.Count - 1]}")]
public class EnergyHistory
{
    public List<EnergyDatapoint> Values { get; set; } = new();
}

[DebuggerDisplay("{InHour} {Energy}")]
public record EnergyDatapoint(
    DateTimeOffset InHour,
    float Energy
);

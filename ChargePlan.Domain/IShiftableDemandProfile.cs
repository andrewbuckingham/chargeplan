namespace ChargePlan.Domain;

public interface IShiftableDemandProfile
{
    string Name { get; }
    string Type { get; }
    TimeOnly Earliest { get; }
    TimeOnly Latest { get; }
    
    /// <summary>
    /// If this is set then the demand can only be made across these date ranges.
    /// The Earliest and Latest times are still observed in combination with this.
    /// </summary>
    (DateTime From, DateTime To)? WithinDayRange { get; }

    TimeSpan? DontRepeatWithin { get; }

    /// <summary>
    /// True if this demand shouldn't be repeated yet.
    /// </summary>
    bool IsTooSoonToRepeat(IShiftableDemandProfile other, DateTime otherDateTime, DateTime thisDateTime)
        => DontRepeatWithin != null && !String.IsNullOrWhiteSpace(Type) && other.Type == Type && (otherDateTime + DontRepeatWithin) > thisDateTime;

    /// <summary>
    /// Sometimes it might be fractionally cheaper to run something later, even though really would like to run it sooner.
    /// This is the threshold for that calculation.
    /// </summary>
    decimal? StartWheneverCheaperThan { get; }

    /// <summary>
    /// Similar to StartWheneverCheaperThan, this is the threshold by which the next day must be cheaper otherwise will run it the previous day.
    /// E.g. if this is zero, and tomorrow is 0.0001p cheaper to run, then it will be delayed until tomorrow.
    /// </summary>
    decimal? NextDayMustSaveAtLeast { get; }

    /// <summary>
    /// Takes into account the value for StartWheneverCheaperThan and NextDayMustSaveAtLeast in order to floor the effective cost.
    /// </summary>
    decimal EffectiveCost(DateTimeOffset algorithmFromDate, DateTimeOffset shiftableDemandTrialDate, decimal calculatedCost)
    {
        // First of all, if it's below the cheapness threshold then consider this zero.
        decimal cost = calculatedCost < StartWheneverCheaperThan ? 0.00M : calculatedCost;

        if (NextDayMustSaveAtLeast != null)
        {
            // Irrespective of the cheapness threshold in a given day; add a penalty amount for later days.
            int days = (int)(shiftableDemandTrialDate.Date - algorithmFromDate.Date).TotalDays;
            cost += (NextDayMustSaveAtLeast ?? decimal.Zero) * days;
        }

        return cost;
    }

    ShiftableDemandPriority Priority { get; }

    IDemandProfile AsDemandProfile(DateTime startingAt);
}

public enum ShiftableDemandPriority
{
    Essential = 0,

    High = 200,

    Medium = 400,

    Low = 600,
}
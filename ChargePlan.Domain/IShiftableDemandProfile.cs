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
public interface IShiftableDemandProfile
{
    string Name { get; }
    TimeOnly Earliest { get; }
    TimeOnly Latest { get; }
    
    /// <summary>
    /// If this is set then the demand can only be made across these date ranges.
    /// The Earliest and Latest times are still observed in combination with this.
    /// </summary>
    (DateTime From, DateTime To)? WithinDayRange { get; }

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
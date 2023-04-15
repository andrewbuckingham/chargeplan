public interface IShiftableDemandProfile
{
    string Name { get; set; }
    TimeOnly Earliest { get; set; }
    TimeOnly Latest { get; set; }
    ShiftableDemandPriority Priority { get; set; }

    IDemandProfile AsDemandProfile(DateTime startingAt);
}

public enum ShiftableDemandPriority
{
    Essential = 0,

    High = 200,

    Medium = 400,

    Low = 600,
}
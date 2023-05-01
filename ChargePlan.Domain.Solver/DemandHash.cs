public static class ShiftableDemandProfileExtensions
{
    public static string AsDemandHash(this IShiftableDemandProfile profile)
        => (profile.WithinDayRange?.From, profile.WithinDayRange?.To, profile.Name).GetHashCode().ToString();
}
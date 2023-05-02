using System.Security.Cryptography;
using System.Text;

public static class ShiftableDemandProfileExtensions
{
    public static string AsDemandHash(this IShiftableDemandProfile profile)
    {
        using var md5 = MD5.Create();

        string str = $"{profile.WithinDayRange?.From.Ticks}{profile.WithinDayRange?.To.Ticks}{profile.Name}";

        var data = md5.ComputeHash(Encoding.Default.GetBytes(str));
        return BitConverter.ToString(data).Replace("-", "").Substring(0, 16);
    }
}
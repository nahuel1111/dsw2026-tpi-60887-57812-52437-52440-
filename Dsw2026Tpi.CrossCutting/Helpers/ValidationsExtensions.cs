using System.Text.RegularExpressions;

namespace Dsw2026Tpi.CrossCutting.Helpers;

public static class ValidationsExtensions
{
    public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$";
    public static bool IsEmailValid(this string? email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
            Regex.IsMatch(email, EmailPattern);
    }

    public static bool IsDniValid(this long? dni)
    {
        if (dni is null || dni <= 0)
            return false;

        var dniStr = dni.Value.ToString();

        return dniStr.Length >= 7 && dniStr.Length <= 8;
    }
}

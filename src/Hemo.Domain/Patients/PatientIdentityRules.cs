using System.Text.RegularExpressions;

namespace Hemo.Domain.Patients;

/// <summary>Non-clinical syntactic rules; clinical identity policy requires hospital approval.</summary>
public static class PatientIdentityRules
{
    public static bool ValidNationalId(string? value) =>
        value is { Length: 10 } && value.All(char.IsAsciiDigit);

    public static string Normalize(string? value) =>
        Regex.Replace((value ?? "").Trim().Replace('ي', 'ی').Replace('ك', 'ک'), @"\s+", " ");

    public static bool Matches(string? actual, string? supplied) =>
        string.Equals(Normalize(actual), Normalize(supplied), StringComparison.Ordinal);
}

using System.Text.RegularExpressions;

namespace SE.TruckTicketing.Contracts.Constants.SourceLocations;

public static class SourceLocationNumberPatterns
{
    public static readonly Regex PlsNumber = new(@"^[A-z]{3}[A-z0-9][- ]\d{1,2}-\d{3}-\d{2,3}$", RegexOptions.Compiled);

    public static readonly Regex ApiNumber = new(@"^\d{10}$", RegexOptions.Compiled);

    public static readonly Regex NorthDakotaWellFileNumber = new(@"^\d{4,5}$", RegexOptions.Compiled);

    public static readonly Regex MontanaWellFileNumber = new(@"^\d{8}$", RegexOptions.Compiled);

    public static readonly Regex CtbNumber = new(@"^\d{6}$", RegexOptions.Compiled);
}

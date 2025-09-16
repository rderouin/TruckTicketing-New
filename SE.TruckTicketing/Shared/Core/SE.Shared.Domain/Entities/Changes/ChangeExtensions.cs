using System.Linq;
using System.Text.RegularExpressions;

namespace SE.Shared.Domain.Entities.Changes;

public static class ChangeExtensions
{
    /// <summary>
    ///     The expression to remove array indexes from the FieldLocation in order to get the agnostic path.
    /// </summary>
    private static readonly Regex ArrayIndexRegex = new(@"\[\d+\]", RegexOptions.Compiled);

    public static string GetAgnosticPath(this ChangeEntity changeEntity)
    {
        return GetAgnosticPath(changeEntity.FieldLocation, changeEntity.FieldName);
    }

    public static string GetAgnosticPath(this FieldChange fieldChange)
    {
        return GetAgnosticPath(fieldChange.FieldLocation, fieldChange.FieldName);
    }

    private static string GetAgnosticPath(string fieldLocation, string fieldName)
    {
        var fullPath = string.Join(".", new[] { fieldLocation, fieldName }.Where(e => !string.IsNullOrEmpty(e)));
        var agnosticPath = ArrayIndexRegex.Replace(fullPath, string.Empty);
        return agnosticPath;
    }
}

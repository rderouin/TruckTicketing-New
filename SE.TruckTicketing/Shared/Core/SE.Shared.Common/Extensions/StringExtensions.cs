using System.Diagnostics;

namespace SE.Shared.Common.Extensions;

public static class StringExtensions
{
    [DebuggerStepThrough]
    public static bool HasText(this string text)
    {
        return !string.IsNullOrWhiteSpace(text);
    }

    [DebuggerStepThrough]
    public static bool HasText(this string text, int minLength)
    {
        minLength = minLength < 0 ? 0 : minLength;
        return HasText(text) && text!.Length >= minLength;
    }

    public static string AsCaseInsensitiveFilterKey(this string key)
    {
        return $"{key}_CI";
    }
}

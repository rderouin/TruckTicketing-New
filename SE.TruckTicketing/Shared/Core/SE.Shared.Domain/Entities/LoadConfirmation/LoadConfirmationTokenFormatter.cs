using System;
using System.Text.RegularExpressions;

using SE.Shared.Common.Extensions;

namespace SE.Shared.Domain.Entities.LoadConfirmation;

public static class LoadConfirmationTokenFormatter
{
    private const string LoadConfirmationPattern = @"(?x)
(?<bracket>\()
    (?<number>[^\(]+?-\w+?)
    :
    (?<hash>[0-9A-F]+?)
(?<-bracket>\))";

    private const string LoadConfirmationPatternLegacy = @"(?xi)(?<hash>[a-f0-9]{16})\s+(?<number>[a-z0-9\-]+)\s*$";

    public static string Format(string lcNumber, string lcHash)
    {
        return $"({lcNumber}:{lcHash})";
    }

    public static (string lcNumber, string lcHash, bool isSuccess) Parse(string input, LoadConfirmationHashStrategy strategy)
    {
        if (input.HasText())
        {
            var pattern = strategy switch
                          {
                              LoadConfirmationHashStrategy.Version1L16 => LoadConfirmationPatternLegacy,
                              LoadConfirmationHashStrategy.Version2L6 => LoadConfirmationPattern,
                              _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null),
                          };

            var match = Regex.Match(input, pattern, RegexOptions.Compiled);
            if (match.Success)
            {
                return (match.Groups["number"].Value, match.Groups["hash"].Value, true);
            }
        }

        return (null, null, false);
    }
}

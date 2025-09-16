using System.Text.RegularExpressions;

namespace SE.BillingService.Domain.InvoiceDelivery;

public static class MustacheParser
{
    // language=regexp
    /// <summary>
    ///     Pattern to extract templated strings, e.g.: {{type:value:option}}
    /// </summary>
    private const string MustachePattern = @"(?x)
(?<m>{{)
    \s* (?<type>\w+) \s*
    :
    \s* (?<value>\S+?) \s*
    
    (?<opt>
        :
        \s* (?<option>\S+?) \s*
    )? 
(?<-m>}})";

    private static readonly Regex MustacheRegex = new(MustachePattern, RegexOptions.Compiled);

    public static (bool success, string type, string value, string option) Match(string source)
    {
        var match = MustacheRegex.Match(source);
        if (match.Success)
        {
            return (true,
                    match.Groups["type"].Success ? match.Groups["type"].Value : null,
                    match.Groups["value"].Success ? match.Groups["value"].Value : null,
                    match.Groups["option"].Success ? match.Groups["option"].Value : null);
        }

        return default;
    }

    public static string Interpolate(string source, MustacheReplaceDelegate replaceDelegate)
    {
        return MustacheRegex.Replace(source, match => match.Success
                                                          ? replaceDelegate(match.Groups["type"].Success ? match.Groups["type"].Value : null,
                                                                            match.Groups["value"].Success ? match.Groups["value"].Value : null,
                                                                            match.Groups["option"].Success ? match.Groups["option"].Value : null) ?? match.Value
                                                          : match.Value);
    }
}

public delegate string MustacheReplaceDelegate(string type, string value, string option);

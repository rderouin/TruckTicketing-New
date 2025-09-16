using System;
using System.Text.RegularExpressions;

namespace SE.Shared.Common.Utilities;

public static class TimeParser
{
    private const string TimeExpression = @"(?inx)
^
	\s*
	(							(?# two options - with a colon and without it )
		(						(?# with a colon option )
			\s*
			(?<h>\d{1,2})		(?# hours )
			\s*
			\:					(?# colon )
			\s*
			(?<m>\d{1,2})		(?# minutes )
			\s*
		)
		|
		(						(?# without a colon option )
			(					(?# hours only )
				\s*
				(?<h>\d{1,2})	(?# hours )
				\s*
			)
			|
			(					(?# hours with minutes )
				\s*
				(?<h>\d{1,2})	(?# hours )
				\s*
				(?<m>\d{2})		(?# minutes, fixed length )
				\s*
			)
		)
	)
	(?<t>
		\s*
		(
			(a\.?\s*m?\.?)		(?# a.m. designator )
			|
			(p\.?\s*m?\.?)		(?# p.m. designator )
		)
		\s*
	)?							(?# am/pm is optional )
	\s*
$
";

    private static readonly Regex _regex = new(TimeExpression, RegexOptions.Compiled);

    public static TimeSpan? Parse(string val)
    {
        // parse the string for a time
        var match = _regex.Match(val);
        if (!match.Success)
        {
            return null;
        }

        // map the groups
        var gh = match.Groups["h"];
        var gm = match.Groups["m"];
        var gt = match.Groups["t"];

        // extract data
        var h = int.Parse(gh.Success ? gh.Value : "0");
        var m = int.Parse(gm.Success ? gm.Value : "0");
        var t = gt.Success ? gt.Value : "am";

        // a.m./p.m. adjustment when in a.m. hours only
        if (t.Contains("p", StringComparison.OrdinalIgnoreCase) && h < 12)
        {
            h += 12;
        }

        // final time object
        var time = new TimeSpan(h, m, 0);
        return time;
    }
}

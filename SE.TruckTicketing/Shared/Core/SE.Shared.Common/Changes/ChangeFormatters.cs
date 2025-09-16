using System;
using System.Collections.Generic;
using System.Linq;

using SE.Shared.Common.Extensions;

namespace SE.Shared.Common.Changes;

public static class ChangeFormatters
{
    public static string FormatCustom(string value, Type type, List<string> args)
    {
        // try parse the value
        var parsedValue = TryParse(value, type, args);

        // format based on the value type
        var formattedValue = parsedValue switch
                             {
                                 DateTimeOffset dto => FormatDateTimeOffset(dto, args),
                                 bool b => FormatBoolean(b, args),
                                 _ => value,
                             };

        return formattedValue;

        static string FormatDateTimeOffset(DateTimeOffset dto, List<string> args)
        {
            // change to UTC
            if (args.Contains("UTC"))
            {
                dto = dto.ToUniversalTime();
            }

            // apply the format string
            if (args.FirstOrDefault(a => a.StartsWith("F=")) is { } formatString)
            {
                return dto.ToString(formatString.Substring(2));
            }

            // no format string defined, fall-back to the ISO 8601 standard
            return dto.ToString("O");
        }

        static string FormatBoolean(bool b, List<string> args)
        {
            return b ? args[0] : args[1];
        }
    }

    public static string FormatDotnet(string value, Type type, List<string> args)
    {
        if (args.FirstOrDefault() is { } formatString && formatString.HasText())
        {
            // try parse the value
            var parsedValue = TryParse(value, type, args);

            // format based on the value type
            value = string.Format($"{{0:{formatString}}}", parsedValue);
        }

        return value;
    }

    private static object TryParse(string value, Type type, List<string> args)
    {
        // support for DateTimeOffset
        if (type == typeof(DateTimeOffset) && DateTimeOffset.TryParse(value, out var dto))
        {
            return dto;
        }

        // support for booleans
        if (type == typeof(bool) && bool.TryParse(value, out var b))
        {
            return b;
        }

        return value;
    }
}
